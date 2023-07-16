namespace SvKEnergyCollectorService.Options;

public class InfluxDbOptions
{
    #region Public Properties

    public string? Bucket { get; set; }
    public string? Measurement { get; set; }
    public string? Organization { get; set; }
    public string? Server { get; set; }
    public string? Token { get; set; }

    #endregion
}
