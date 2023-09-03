namespace EntsoeCollectorService;

using Measurements;

public class EntsoeCollectorService : IEntsoeCollectorService
{
    #region Fields

    private readonly DayAheadPriceMeasurements _dayAheadPriceMeasurements;
    private readonly GenerateMeasurements _generateMeasurements;
    private readonly LoadMeasurements _loadMeasurements;
    private readonly PhysicalFlowMeasurements _physicalFlowMeasurements;

    #endregion

    #region Constructors and Destructors

    public EntsoeCollectorService(GenerateMeasurements generateMeasurements, LoadMeasurements loadMeasurements, DayAheadPriceMeasurements dayAheadPriceMeasurements, PhysicalFlowMeasurements physicalFlowMeasurements)
    {
        _generateMeasurements = generateMeasurements;
        _loadMeasurements = loadMeasurements;
        _dayAheadPriceMeasurements = dayAheadPriceMeasurements;
        _physicalFlowMeasurements = physicalFlowMeasurements;
    }

    #endregion

    #region Public Methods and Operators

    public async Task Run(CancellationToken cancellationToken)
    {
        await _generateMeasurements.SyncData(cancellationToken);
        await _loadMeasurements.SyncData(cancellationToken);
        await _dayAheadPriceMeasurements.SyncData(cancellationToken);
        await _physicalFlowMeasurements.SyncData(cancellationToken);
    }

    #endregion
}
