namespace EnergyCollectorService.InfluxDb.Options;

public class InfluxDbOptions
{
    #region Public Properties

    public string? Bucket { get; set; }
    public string? DayAheadPriceMeasurement { get; set; }
    public string? GenerateMeasurement { get; set; }
    public string? LoadMeasurement { get; set; }
    public string? Organization { get; set; }
    public string? PhysicalFlowMeasurement { get; set; }
    public string? Server { get; set; }
    public string? Token { get; set; }

    #endregion
}
