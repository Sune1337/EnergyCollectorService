namespace EntsoeCollectorService;

using global::EntsoeCollectorService.Measurements;

public class EntsoeCollectorService : IEntsoeCollectorService
{
    #region Fields

    private readonly DayAheadPriceMeasurements _dayAheadPriceMeasurements;

    private readonly EnergyMeasurements _energyMeasurements;

    #endregion

    #region Constructors and Destructors

    public EntsoeCollectorService(EnergyMeasurements energyMeasurements, DayAheadPriceMeasurements dayAheadPriceMeasurements)
    {
        _energyMeasurements = energyMeasurements;
        _dayAheadPriceMeasurements = dayAheadPriceMeasurements;
    }

    #endregion

    #region Public Methods and Operators

    public async Task Run(CancellationToken cancellationToken)
    {
        await _energyMeasurements.SyncEnergyData(cancellationToken);
        await _dayAheadPriceMeasurements.SyncDayAheadPriceData(cancellationToken);
    }

    #endregion
}
