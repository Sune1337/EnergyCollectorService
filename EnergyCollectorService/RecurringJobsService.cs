namespace EnergyCollectorService;

using EnergyCollectorService.Options;

using EntsoeCollectorService;

using Hangfire;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SvKEnergyCollectorService;

public class RecurringJobsService : IRecurringJobsService
{
    #region Fields

    private readonly ILogger<RecurringJobsService> _logger;

    private readonly IRecurringJobManager _recurringJobManager;

    private readonly IOptions<ScheduledJobsOptions> _scheduledJobs;

    #endregion

    #region Constructors and Destructors

    public RecurringJobsService(IOptions<ScheduledJobsOptions> scheduledJobs, IRecurringJobManager recurringJobManager, ILogger<RecurringJobsService> logger)
    {
        _scheduledJobs = scheduledJobs;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    #endregion

    #region Public Methods and Operators

    public void ConfigureRecurringJobs()
    {
        if (_scheduledJobs.Value.Jobs == null)
        {
            return;
        }

        foreach (var recurringJob in _scheduledJobs.Value.Jobs)
        {
            switch (recurringJob.JobId)
            {
                case "SvKEnergyCollectorService":
                    // Register a recurring job to run SvKEnergyCollectorService.
                    _recurringJobManager.AddOrUpdate<ISvKEnergyCollectorService>(recurringJob.JobId, x => x.Run(CancellationToken.None), recurringJob.CronExpression);
                    break;
                
                case "EntsoeCollectorService":
                    // Register a recurring job to run EntsoeCollectorService.
                    _recurringJobManager.AddOrUpdate<IEntsoeCollectorService>(recurringJob.JobId, x => x.Run(CancellationToken.None), recurringJob.CronExpression);
                    break;
            }

            if (recurringJob.TriggerImmediately)
            {
                // Trigger the job to run once immediately.
                _recurringJobManager.Trigger(recurringJob.JobId);
            }
        }
    }

    #endregion
}
