namespace EntsoeCollectorService.Measurements;

using System.Globalization;

using EnergyCollectorService.InfluxDb.Models;
using EnergyCollectorService.InfluxDb.Options;

using global::EntsoeCollectorService.Configuration;
using global::EntsoeCollectorService.EntsoeApi;
using global::EntsoeCollectorService.EntsoeApi.Models.Generationload;
using global::EntsoeCollectorService.Utils;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Refit;

public class EnergyMeasurements
{
    #region Static Fields

    private static readonly Dictionary<string, string> EnergyTypeLookup = new()
    {
        // B01 Biomass
        { "B01", "Värmekraft" },
        // B02 Fossil Brown coal/Lignite
        { "B02", "Värmekraft" },
        // B03 Fossil Coal-derived gas
        { "B03", "Värmekraft" },
        // B04 Fossil Gas
        { "B04", "Värmekraft" },
        // B05 Fossil Hard coal
        { "B05", "Värmekraft" },
        // B06 Fossil Oil
        { "B06", "Värmekraft" },
        // B07 Fossil Oil shale
        { "B07", "Värmekraft" },
        // B08 Fossil Peat
        { "B08", "Värmekraft" },
        // B09 Geothermal
        { "B09", "Värmekraft" },
        // B10 Hydro Pumped Storage
        { "B10", "Vattenkraft" },
        // B11 Hydro Run-of-river and poundage
        { "B11", "Vattenkraft" },
        // B12 Hydro Water Reservoir
        { "B12", "Vattenkraft" },
        // B13 Marine
        { "B13", "Ospecificerat" },
        // B14 Nuclear
        { "B14", "Kärnkraft" },
        // B15 Other renewable
        { "B15", "Ospecificerat" },
        // B16 Solar
        { "B16", "Solkraft" },
        // B17 Waste
        { "B17", "Värmekraft" },
        // B18 Wind Offshore
        { "B18", "Vindkraft" },
        // B19 Wind Onshore
        { "B19", "Vindkraft" },
        // B20 Other
        { "B20", "Ospecificerat" },
        // B21 AC Link
        { "B21", "Ospecificerat" },
        // B22 DC Link
        { "B22", "Ospecificerat" },
        // B23 Substation
        { "B23", "Ospecificerat" },
        // B24 Transformer
        { "B24", "Ospecificerat" },
    };

    #endregion

    #region Fields

    private readonly IEntsoeApiClient _entsoeApiClient;
    private readonly IOptions<InfluxDbOptions> _influxDbOptions;
    private readonly ILogger<EntsoeCollectorService> _logger;
    private readonly IOptions<EntsoeApiOptions> _options;

    #endregion

    #region Constructors and Destructors

    public EnergyMeasurements(ILogger<EntsoeCollectorService> logger, IEntsoeApiClient entsoeApiClient, IOptions<EntsoeApiOptions> options, IOptions<InfluxDbOptions> influxDbOptions)
    {
        _logger = logger;
        _entsoeApiClient = entsoeApiClient;
        _options = options;
        _influxDbOptions = influxDbOptions;
    }

    #endregion

    #region Public Methods and Operators

    public async Task SyncEnergyData(CancellationToken cancellationToken)
    {
        // Create InfluxDB client.
        using var influxDBClient = new InfluxDBClient(_influxDbOptions.Value.Server, _influxDbOptions.Value.Token);

        // Get influx reader
        var influxQuery = influxDBClient.GetQueryApi();

        // Find out what the latest date is that we've previously stored.
        var from = (
            (
                await influxQuery.QueryAsync<InfluxData>($@"from(bucket: ""{_influxDbOptions.Value.Bucket}"")
  |> range(start: -10y, stop: now())
  |> filter(fn: (r) => r[""_measurement""] == ""{_influxDbOptions.Value.EnergyMeasurement}"")
  |> group()
  |> last(column: ""_time"")
", _influxDbOptions.Value.Organization, cancellationToken)
            )
            .FirstOrDefault()?.Time ?? DateTime.Now.AddYears(-10)
        ).Date;

        // Get influx writer.
        using var influxWrite = influxDBClient.GetWriteApi();
        influxWrite.EventHandler += InfluxWriteEventHandler;

        var startDateTime = from.Date;
        var timeSpan = TimeSpan.FromDays(7);

        var currentDate = startDateTime;
        while (currentDate < DateTime.Now)
        {
            await DownloadEnergyData(currentDate, timeSpan, influxWrite, cancellationToken);
            currentDate = currentDate.Add(timeSpan);
        }
    }

    #endregion

    #region Methods

    private async Task DownloadEnergyData(DateTime startDateTime, TimeSpan timeSpan, WriteApi influxWrite, CancellationToken cancellationToken)
    {
        const string seDomain = "10YSE-1--------K";

        GL_MarketDocument? data;
        try
        {
            data = await _entsoeApiClient.ActualGenerationPerProductionType(_options.Value.AccessToken, seDomain, startDateTime, startDateTime.Add(timeSpan), cancellationToken);
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

        var pointsPerDateTime = data.TimeSeries
            .SelectMany(ts => EnumeratePeriodPoints(ts,
                    (energyType, psrType, dateTime, point) => new
                    {
                        energyType, psrType, dateTime, point.quantity
                    }
                )
            )
            .GroupBy(x => x.dateTime);

        // Parse data and iterate all points.
        foreach (var pointsForDateTime in pointsPerDateTime)
        {
            var totalQuantity = 0m;
            var pointsPerEnergyType = pointsForDateTime
                .GroupBy(p => p.energyType);
            foreach (var pointsForEnergyType in pointsPerEnergyType)
            {
                var quantityPerEnergyType = 0m;
                var minDateTime = DateTime.MaxValue;
                foreach (var point in pointsForEnergyType)
                {
                    quantityPerEnergyType += point.quantity;
                    if (point.dateTime < minDateTime)
                    {
                        minDateTime = point.dateTime;
                    }
                }

                // Save quantityPerEnergyType to InfluxDb.
                influxWrite.WritePoint(
                    PointData.Measurement(_influxDbOptions.Value.EnergyMeasurement)
                        .Tag("measurements", pointsForEnergyType.Key)
                        .Field("value", quantityPerEnergyType)
                        .Timestamp(minDateTime, WritePrecision.Ns)
                    , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
                );

                totalQuantity += quantityPerEnergyType;
            }

            // Save totalQuantity to InfluxDb.
            influxWrite.WritePoint(
                PointData.Measurement(_influxDbOptions.Value.EnergyMeasurement)
                    .Tag("measurements", "Total produktion")
                    .Field("value", totalQuantity)
                    .Timestamp(pointsForDateTime.Key, WritePrecision.Ns)
                , _influxDbOptions.Value.Bucket, _influxDbOptions.Value.Organization
            );
        }
    }

    private IEnumerable<T> EnumeratePeriodPoints<T>(TimeSeries? timeSerie, Func<string, string, DateTime, Point, T> func)
    {
        if (timeSerie == null)
        {
            yield break;
        }

        var psrType = timeSerie.MktPSRType.psrType;
        if (!EnergyTypeLookup.TryGetValue(psrType, out var energyType))
        {
            _logger.LogWarning("Missing mapping from {PsrType} to energyType.", psrType);
            yield break;
        }

        foreach (var period in timeSerie.Period)
        {
            // Iterate datapointes.
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

                yield return func(energyType, psrType, pointDateTime, point);
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

    #endregion
}
