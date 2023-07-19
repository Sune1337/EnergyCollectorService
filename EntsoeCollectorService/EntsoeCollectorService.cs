namespace EntsoeCollectorService;

using Measurements;

public class EntsoeCollectorService : IEntsoeCollectorService
{
    #region Fields

    private readonly DayAheadPriceMeasurements _dayAheadPriceMeasurements;

    private readonly GenerateMeasurements _generateMeasurements;
    private readonly LoadMeasurements _loadMeasurements;

    #endregion

    #region Constructors and Destructors

    public EntsoeCollectorService(GenerateMeasurements generateMeasurements, LoadMeasurements loadMeasurements, DayAheadPriceMeasurements dayAheadPriceMeasurements)
    {
        _generateMeasurements = generateMeasurements;
        _loadMeasurements = loadMeasurements;
        _dayAheadPriceMeasurements = dayAheadPriceMeasurements;
    }

    #endregion

    #region Public Methods and Operators

    public async Task Run(CancellationToken cancellationToken)
    {
        await _generateMeasurements.SyncData(cancellationToken);
        await _loadMeasurements.SyncData(cancellationToken);
        await _dayAheadPriceMeasurements.SyncData(cancellationToken);
    }

    #endregion
}