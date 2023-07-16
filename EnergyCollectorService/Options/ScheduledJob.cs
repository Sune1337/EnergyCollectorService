namespace EnergyCollectorService.Options;

public class ScheduledJob
{
    #region Public Properties

    public string? CronExpression { get; set; }
    public string? JobId { get; set; }
    public bool TriggerImmediately { get; set; }

    #endregion
}
