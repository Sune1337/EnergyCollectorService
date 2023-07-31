using System.Globalization;
using EnergyCollectorService.CurrencyConversion;
using EnergyCollectorService.InfluxDb.Models;
using EnergyCollectorService.InfluxDb.Options;
using EnergyCollectorService.Utils;
using EntsoeCollectorService.Configuration;
using EntsoeCollectorService.EntsoeApi;
using EntsoeCollectorService.EntsoeApi.Models.Generationload;
using EntsoeCollectorService.Utils;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace EntsoeCollectorService.Measurements;

public class LoadMeasurements
{
    #region Fields

    private readonly ICurrencyConvert _currencyConvert;

    private readonly IEntsoeApiClient _entsoeApiClient;
    private readonly IOptions<InfluxDbOptions> _influxDbOptions;
    private readonly ILogger<EntsoeCollectorService> _logger;
    private readonly IOptions<EntsoeApiOptions> _options;

    #endregion

    #region Constructors and Destructors

    public LoadMeasurements(ILogger<EntsoeCollectorService> logger, IEntsoeApiClient entsoeApiClient, IOptions<EntsoeApiOptions> options, IOptions<InfluxDbOptions> influxDbOptions, ICurrencyConvert currencyConvert)
    {
        _logger = logger;
        _entsoeApiClient = entsoeApiClient;
        _options = options;
        _influxDbOptions = influxDbOptions;
        _currencyConvert = currencyConvert;
    }

    #endregion

    #region Public Methods and Operators

    public async Task SyncData(CancellationToken cancellationToken)
    {
        // Create InfluxDB client.
        using var influxDBClient = new InfluxDBClient(new InfluxDBClientOptions(_influxDbOptions.Value.Server)
        {
            Token = _influxDbOptions.Value.Token,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Get influx reader
        var queryApi = influxDBClient.GetQueryApi();

        // Get influx writer.
        using var writeApi = influxDBClient.GetWriteApi();
        writeApi.EventHandler += InfluxWriteEventHandler;

        // Sync new data.
        foreach (var areaKeyValue in EntsoeCodes.Areas)
        {
            await SyncLoadForArea(areaKeyValue, queryApi, writeApi, cancellationToken);
        }
    }

    #endregion

    #region Methods

    private async Task DownloadLoadData(DateTime startDateTime, TimeSpan timeSpan, KeyValuePair<string, string> areaKeyValue, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        GL_MarketDocument? data;
        try
        {
            data = await _entsoeApiClient.ActualLoadPerProductionType(_options.Value.AccessToken, areaKeyValue.Key, startDateTime, startDateTime.Add(timeSpan), cancellationToken);
        }

        catch (ApiException ex)
        {
            // Try to parse acknowledgement document to check for errors.
            var acknowledgement = AcknowledgementMarketDocumentParser.ParseXml(ex.Content);
            if (acknowledgement?.Reason?.text?.StartsWith("No matching data found") == true)
            {
                // No data found for the current query.
                return;
            }

            throw;
        }

        var points = data.TimeSeries
            .SelectMany(ts => EnumeratePeriodPoints(ts,
                    (dateTime, point) => new
                    {
                        dateTime, point.quantity
                    }
                )
            );

        // Parse data and iterate all points.
        foreach (var point in points)
        {
            // Save data to InfluxDb.
            influxWrite.WritePoint(
                PointData.Measurement(_influxDbOptions.Value.LoadMeasurement)
                    .Tag("area", areaKeyValue.Value)
                    .Field("MW", point.quantity)
                    .Timestamp(point.dateTime, WritePrecision.Ns)
                , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
            );
        }
    }

    private IEnumerable<T> EnumeratePeriodPoints<T>(TimeSeries? timeSerie, Func<DateTime, Point, T> func)
    {
        if (timeSerie == null)
        {
            yield break;
        }

        foreach (var period in timeSerie.Period)
        {
            // Iterate datapoints.
            if (!DateTime.TryParse(period.timeInterval.start, CultureInfo.InvariantCulture, out var firstDateTime))
            {
                _logger.LogWarning("Could not parse period {start} as DateTime", period.timeInterval.start);
                continue;
            }

            if (!period.resolution.TryParseIso8601DurationString(out var resolution))
            {
                _logger.LogWarning("Could not parse resolution {resolution} as Iso8601 duration", period.resolution);
                continue;
            }

            foreach (var point in period.Point)
            {
                if (!long.TryParse(point.position, out var position))
                {
                    _logger.LogWarning("Could not parse position {position} as a long", point.position);
                    break;
                }

                var pointDateTime = firstDateTime.Add(resolution * (position - 1));

                yield return func(pointDateTime, point);
            }
        }
    }

    private void InfluxWriteEventHandler(object? sender, EventArgs eventArgs)
    {
        switch (eventArgs)
        {
            case WriteErrorEvent error:
                _logger.LogError(error.Exception.Message);
                break;
            case WriteRetriableErrorEvent error:
                _logger.LogError(error.Exception.Message);
                break;
            case WriteRuntimeExceptionEvent error:
                _logger.LogError(error.Exception.Message);
                break;
        }
    }

    private async Task SyncLoadForArea(KeyValuePair<string, string> areaKeyValue, QueryApi influxQuery, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        // Find out what the latest date is that we've previously stored.
        var from = (
            (
                await influxQuery.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: -10y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.LoadMeasurement}"" and r[""area""] == ""{areaKeyValue.Value}"")
  |> group()
  |> last(column: ""_time"")
", _influxDbOptions.Value.Organization, cancellationToken)
            )
            .FirstOrDefault()?.Time ?? BeginningOfTime.DateTime
        ).Date;

        var startDateTime = from.Date;
        var timeSpan = TimeSpan.FromDays(7);

        var currentDate = startDateTime;
        while (currentDate < DateTime.Now)
        {
            await DownloadLoadData(currentDate, timeSpan, areaKeyValue, influxWrite, cancellationToken);
            currentDate = currentDate.Add(timeSpan);
        }
    }

    #endregion
}
