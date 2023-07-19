namespace EnergyCollectorService.InfluxDb.Models;

public class InfluxData
{
    #region Public Properties

    public DateTime Time { get; init; }

    public decimal Value { get; init; }

    #endregion
}