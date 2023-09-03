using System.Globalization;
using EnergyCollectorService.InfluxDb.Models;
using EnergyCollectorService.InfluxDb.Options;
using EnergyCollectorService.Utils;
using EntsoeCollectorService.Configuration;
using EntsoeCollectorService.EntsoeApi;
using EntsoeCollectorService.EntsoeApi.Models.Publication;
using EntsoeCollectorService.Utils;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace EntsoeCollectorService.Measurements;

public class PhysicalFlowMeasurements
{
    #region Fields

    private readonly IEntsoeApiClient _entsoeApiClient;
    private readonly IOptions<InfluxDbOptions> _influxDbOptions;
    private readonly ILogger<EntsoeCollectorService> _logger;
    private readonly IOptions<EntsoeApiOptions> _options;

    #endregion

    #region Constructors and Destructors

    public PhysicalFlowMeasurements(ILogger<EntsoeCollectorService> logger, IEntsoeApiClient entsoeApiClient, IOptions<EntsoeApiOptions> options, IOptions<InfluxDbOptions> influxDbOptions)
    {
        _logger = logger;
        _entsoeApiClient = entsoeApiClient;
        _options = options;
        _influxDbOptions = influxDbOptions;
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
        foreach (var areaKeyValue in EntsoeCodes.Areas.Where(a => a.CountryCode == "SE"))
        {
            await SyncPhysicalFlowForArea(areaKeyValue, queryApi, writeApi, cancellationToken);
        }
    }

    #endregion

    #region Methods

    private static Task<(T[] Results, Exception[] Exceptions)> WhenAllEx<T>(
        IEnumerable<Task<T>> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        tasks = tasks.ToArray(); // Defensive copy
        return Task.WhenAll(tasks).ContinueWith(task => // return a continuation of WhenAll
            {
                var results = tasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .Select(t => t.Result)
                    .ToArray();
                var aggregateExceptions = tasks
                    .Where(t => t.IsFaulted)
                    .Select(t => t.Exception) // The Exception is of type AggregateException
                    .ToArray();
                var exceptions = new AggregateException(aggregateExceptions).Flatten()
                    .InnerExceptions.ToArray(); // Flatten the hierarchy of AggregateExceptions
                if (exceptions.Length == 0 && task.IsCanceled)
                {
                    // No exceptions and at least one task was canceled
                    exceptions = new[] { new TaskCanceledException(task) };
                }

                return (results, exceptions);
            }, default, TaskContinuationOptions.DenyChildAttach |
                        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task DownloadPhysicalFlowData(DateTime startDateTime, TimeSpan timeSpan, EntsoeArea areaKeyValue, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        var tasks = GenerateTransferPairs(areaKeyValue.Description)
            .Select(pair => _entsoeApiClient.PhysicalFlows(_options.Value.AccessToken, EntsoeCodes.AreaDescriptionLookup[pair.source].Code, EntsoeCodes.AreaDescriptionLookup[pair.destination].Code, startDateTime, startDateTime.Add(timeSpan), cancellationToken));

        var (datas, exceptions) = await WhenAllEx(tasks);

        // Iterate thrown exceptions.
        foreach (var ex in exceptions)
        {
            if (ex is not ApiException apiException)
            {
                throw ex;
            }

            // Try to parse acknowledgement document to check for errors.
            var acknowledgement = AcknowledgementMarketDocumentParser.ParseXml(apiException.Content);
            if (acknowledgement?.Reason?.text?.StartsWith("No matching data found") != true)
            {
                // No data found for the current query.
                throw ex;
            }
        }

        foreach (var data in datas)
        {
            foreach (var timeSerie in data.TimeSeries)
            {
                var points = EnumeratePeriodPoints(timeSerie,
                    (dateTime, point) => new
                    {
                        dateTime, point.quantity
                    }
                );

                var isOut = timeSerie.out_DomainmRID.Value == areaKeyValue.Code;
                var modifier = isOut ? -1m : 1m;
                var otherArea = EntsoeCodes.AreaCodeLookup[isOut ? timeSerie.in_DomainmRID.Value : timeSerie.out_DomainmRID.Value].Description;

                // Parse data and iterate all points.
                foreach (var point in points)
                {
                    // Save data to InfluxDb.
                    influxWrite.WritePoint(
                        PointData.Measurement(_influxDbOptions.Value.PhysicalFlowMeasurement)
                            .Tag("area", areaKeyValue.Description)
                            .Tag("otherArea", otherArea)
                            .Tag("direction", isOut ? "Out" : "In")
                            .Field("MW", point.quantity * modifier)
                            .Timestamp(point.dateTime, WritePrecision.Ns)
                        , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
                    );
                }
            }
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

    private IEnumerable<(string source, string destination)> GenerateTransferPairs(string sourceArea)
    {
        var neighbours = EntsoeCodes.TransferNeighbours[sourceArea];
        foreach (var neighbour in neighbours)
        {
            yield return (sourceArea, neighbour);
            yield return (neighbour, sourceArea);
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

    private async Task SyncPhysicalFlowForArea(EntsoeArea areaKeyValue, QueryApi influxQuery, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        // Find out what the latest date is that we've previously stored.
        var from = (
            (
                await influxQuery.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: -10y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.PhysicalFlowMeasurement}"" and r[""area""] == ""{areaKeyValue.Description}"")
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
            await DownloadPhysicalFlowData(currentDate, timeSpan, areaKeyValue, influxWrite, cancellationToken);
            currentDate = currentDate.Add(timeSpan);
        }
    }

    #endregion
}
