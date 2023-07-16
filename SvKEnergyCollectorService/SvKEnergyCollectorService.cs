namespace SvKEnergyCollectorService;

using Microsoft.Extensions.Logging;

public class SvKEnergyCollectorService : ISvKEnergyCollectorService
{
    #region Fields

    private readonly ILogger<SvKEnergyCollectorService> _logger;

    #endregion

    #region Constructors and Destructors

    public SvKEnergyCollectorService(ILogger<SvKEnergyCollectorService> logger)
    {
        _logger = logger;
    }

    #endregion

    #region Public Methods and Operators

    public async Task Run(CancellationToken cancellationToken)
    {
    }

    #endregion
}
