namespace EnergyCollectorService.InfluxDb.Models;

public class ProductionData
{
    #region Public Properties

    public XYStatistics[] Data { get; set; } = null!;

    public string ID { get; set; } = null!;

    #endregion
}
