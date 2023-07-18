namespace SvKEnergyCollectorService;

using System.Net.Http.Json;
using System.Text.Json;

using EnergyCollectorService.InfluxDb.Models;
using EnergyCollectorService.InfluxDb.Options;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class SvKEnergyCollectorService : ISvKEnergyCollectorService
{
    #region Static Fields

    private static readonly Dictionary<string, string> EnergyTypeLookup = new()
    {
        { "1", "Total produktion" },
        { "2", "Kärnkraft" },
        { "3", "Vattenkraft" },
        { "4", "Värmekraft" },
        { "5", "Vindkraft" },
        { "6", "Ospecificerat" },
        { "7", "Total förbrukning" },
        { "8", "Netto export" }
    };

    #endregion

    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SvKEnergyCollectorService> _logger;
    private readonly IOptions<InfluxDbOptions> _options;

    #endregion

    #region Constructors and Destructors

    public SvKEnergyCollectorService(ILogger<SvKEnergyCollectorService> logger, IHttpClientFactory httpClientFactory, IOptions<InfluxDbOptions> options)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    #endregion

    #region Public Methods and Operators

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            // Get a HttpClient.
            using var httpClient = _httpClientFactory.CreateClient("SvKAPI");

            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            jsonSerializerOptions.Converters.Add(new DateTimeConverter());

            // Create InfluxDB client.
            using var influxDBClient = new InfluxDBClient(_options.Value.Server, _options.Value.Token);

            // Get influx reader
            var influxQuery = influxDBClient.GetQueryApi();

            // Find out what the latest date is that we've previously stored.
            var from = (
                (
                    await influxQuery.QueryAsync<InfluxData>($@"from(bucket: ""{_options.Value.Bucket}"")
  |> range(start: -1y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_options.Value.EnergyMeasurement}"")
  |> group()
  |> last(column: ""_time"")
", _options.Value.Organization, cancellationToken)
                )
                .FirstOrDefault()?.Time ?? DateTime.Now.AddYears(-10)
            ).Date;

            // Get influx writer.
            using var influxWrite = influxDBClient.GetWriteApi();
            influxWrite.EventHandler += InfluxWriteEventHandler;

            // Initialize dates.
            var to = DateTime.Now.Date;
            var currentDate = from;

            // Start downloading first data.
            var nextDownloadTask = httpClient.GetFromJsonAsync<ProductionResponse>($"/services/controlroom/v2/production?date={currentDate:yyyy-MM-dd}&countryCode=SE", jsonSerializerOptions, cancellationToken: cancellationToken);

            // Iterate all dates to download.
            while (currentDate <= to)
            {
                // Wait for current download to finish.
                var jsonData = await nextDownloadTask;

                // Download next data while we're processing the current.
                currentDate = currentDate.AddDays(1);
                if (currentDate <= to)
                {
                    nextDownloadTask = httpClient.GetFromJsonAsync<ProductionResponse>($"/services/controlroom/v2/production?date={currentDate:yyyy-MM-dd}&countryCode=SE", jsonSerializerOptions, cancellationToken: cancellationToken);
                }

                // Iterate data.
                var data = jsonData?.Data;
                if (!(data?.Length > 0))
                {
                    // There were no data in current file. Proceed to next date.
                    continue;
                }

                // Iterate all energy-types.
                foreach (var productionSource in data)
                {
                    if (EnergyTypeLookup.TryGetValue(productionSource.ID, out var energyType) == false)
                    {
                        throw new Exception($"Could not find id {productionSource.ID} in EnergyTypeLookup.");
                    }

                    // Iterate all data in current source.
                    foreach (var xy in productionSource.Data)
                    {
                        influxWrite.WritePoint(
                            PointData.Measurement(_options.Value.EnergyMeasurement)
                                .Tag("measurements", energyType)
                                .Field("value", xy.Y)
                                .Timestamp(xy.X, WritePrecision.Ns)
                            , _options.Value.Bucket, _options.Value.Organization
                        );
                    }
                }
            }
        }

        catch (OperationCanceledException)
        {
            _logger.LogWarning("Application was cancelled.");
        }

        // Data will be flushed to InfluxDB when the objects are disposed.
        _logger.LogInformation("Flushing data to InfluxDB.");
    }

    #endregion

    #region Methods

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

    #endregion
}
