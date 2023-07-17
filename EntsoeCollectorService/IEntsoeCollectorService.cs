namespace EntsoeCollectorService;

using Hangfire;

public interface IEntsoeCollectorService
{
    #region Public Methods and Operators

    [DisableConcurrentExecution(0)]
    Task Run(CancellationToken cancellationToken);

    #endregion
}
