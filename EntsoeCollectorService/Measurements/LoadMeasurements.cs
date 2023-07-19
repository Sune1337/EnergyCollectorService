namespace EntsoeCollectorService.Measurements;

using System.Globalization;
using System.Xml;
using Configuration;
using EnergyCollectorService.CurrencyConversion;
using EnergyCollectorService.InfluxDb.Models;
using EnergyCollectorService.InfluxDb.Options;
using EntsoeApi;
using EntsoeApi.Models.Generationload;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using Utils;

public class LoadMeasurements
{
    #region Static Fields

    private static readonly Dictionary<string, string> AreaLookup = new()
    {
        // 10Y1001A1001A44P IPA|SE1, BZN|SE1, MBA|SE1, SCA|SE1
        { "10Y1001A1001A44P", "SE1" },
        // 10Y1001A1001A45N SCA|SE2, MBA|SE2, BZN|SE2, IPA|SE2
        { "10Y1001A1001A45N", "SE2" },
        // 10Y1001A1001A46L IPA|SE3, BZN|SE3, MBA|SE3, SCA|SE3
        { "10Y1001A1001A46L", "SE3" },
        // 10Y1001A1001A47J SCA|SE4, MBA|SE4, BZN|SE4, IPA|SE4
        { "10Y1001A1001A47J", "SE4" }
    };

    #endregion

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
        DateTime? minFirstDate = null, minLastDate = null;
        foreach (var areaKeyValue in AreaLookup)
        {
            var (currentFirstDate, currentLastDate) = await SyncLoadForArea(areaKeyValue, queryApi, writeApi, cancellationToken);
            if (minFirstDate == null || currentFirstDate < minFirstDate)
            {
                minFirstDate = currentFirstDate;
            }

            if (minLastDate == null || currentLastDate < minLastDate)
            {
                minLastDate = currentLastDate;
            }
        }

        // Calculate new total load.
        if (minFirstDate != null && minLastDate != null)
        {
            // Make sure all written data is flushed.
            writeApi.Flush();

            var start = XmlConvert.ToString(minFirstDate.Value, XmlDateTimeSerializationMode.Utc);

            var lastCalculatedLoadData = await queryApi.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: -10y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.LoadMeasurement}"" and r[""measurements""] == ""Total last"")
  |> group()
  |> last(column: ""_time"")
", _influxDbOptions.Value.Organization, cancellationToken);

            var lastTotalLoadDateTime = lastCalculatedLoadData.FirstOrDefault()?.Time;
            var calculateFromDateTime = lastTotalLoadDateTime == null
                ? DateTime.Now.AddYears(-10).Date
                : minFirstDate < lastTotalLoadDateTime
                    ? minFirstDate.Value
                    : lastTotalLoadDateTime.Value;

            start = XmlConvert.ToString(calculateFromDateTime, XmlDateTimeSerializationMode.Utc);
            var stop = XmlConvert.ToString(minLastDate.Value.AddMicroseconds(1), XmlDateTimeSerializationMode.Utc);
            var loadData = await queryApi.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: {start}, stop: {stop})
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.LoadMeasurement}"" and r[""_field""] == ""value"" and r[""measurements""] != ""Total last"")
  |> group(columns: [""_time""])
  |> sum(column: ""_value"")
", _influxDbOptions.Value.Organization, cancellationToken);

            foreach (var data in loadData)
            {
                // Save data to InfluxDb.
                writeApi.WritePoint(
                    PointData.Measurement(_influxDbOptions.Value.LoadMeasurement)
                        .Tag("measurements", "Total last")
                        .Field("value", data.Value)
                        .Timestamp(data.Time, WritePrecision.Ns)
                    , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
                );
            }
        }
    }

    #endregion

    #region Methods

    private async Task<(DateTime? minFirstDate, DateTime? minLastDate)> DownloadLoadData(DateTime startDateTime, TimeSpan timeSpan, KeyValuePair<string, string> areaKeyValue, WriteApi influxWrite, CancellationToken cancellationToken)
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
                return (null, null);
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
        DateTime? minDate = null, maxDate = null;
        foreach (var point in points)
        {
            if (minDate == null || point.dateTime < minDate)
            {
                minDate = point.dateTime;
            }

            if (maxDate == null || point.dateTime > maxDate)
            {
                maxDate = point.dateTime;
            }

            // Save data to InfluxDb.
            influxWrite.WritePoint(
                PointData.Measurement(_influxDbOptions.Value.LoadMeasurement)
                    .Tag("measurements", areaKeyValue.Value)
                    .Field("value", point.quantity)
                    .Timestamp(point.dateTime, WritePrecision.Ns)
                , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
            );
        }

        return (minDate, maxDate);
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

    private async Task<(DateTime? firstDate, DateTime? lastDate)> SyncLoadForArea(KeyValuePair<string, string> areaKeyValue, QueryApi influxQuery, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        // Find out what the latest date is that we've previously stored.
        var from = (
            (
                await influxQuery.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: -10y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.LoadMeasurement}"" and r[""measurements""] == ""{areaKeyValue.Value}"")
  |> group()
  |> last(column: ""_time"")
", _influxDbOptions.Value.Organization, cancellationToken)
            )
            .FirstOrDefault()?.Time ?? DateTime.Now.AddYears(-10)
        ).Date;

        var startDateTime = from.Date;
        var timeSpan = TimeSpan.FromDays(7);

        var currentDate = startDateTime;
        DateTime? firstDate = null, lastDate = null;
        while (currentDate < DateTime.Now)
        {
            var (currentFirstDate, currentLastDate) = await DownloadLoadData(currentDate, timeSpan, areaKeyValue, influxWrite, cancellationToken);
            if (firstDate == null || currentFirstDate < firstDate)
            {
                firstDate = currentFirstDate;
            }

            if (lastDate == null || currentLastDate > lastDate)
            {
                lastDate = currentLastDate;
            }

            currentDate = currentDate.Add(timeSpan);
        }

        return (firstDate, lastDate);
    }

    #endregion
}