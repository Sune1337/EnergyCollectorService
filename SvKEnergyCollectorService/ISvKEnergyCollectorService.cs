namespace SvKEnergyCollectorService;

using Hangfire;

public interface ISvKEnergyCollectorService
{
    #region Public Methods and Operators

    [DisableConcurrentExecution(0)]
    Task Run(CancellationToken cancellationToken);

    #endregion
}
