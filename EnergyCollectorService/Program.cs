// Create host.

using EnergyCollectorService;
using EnergyCollectorService.InfluxDb.Options;
using EnergyCollectorService.Options;

using EntsoeCollectorService;
using EntsoeCollectorService.Configuration;

using Hangfire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SvKEnergyCollectorService;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, configurationBuilder) =>
        configurationBuilder.AddJsonFile("appsettings.json")
    )
    .ConfigureServices((hostBuilderContext, services) =>
    {
        // Bind options.
        services.Configure<InfluxDbOptions>(hostBuilderContext.Configuration.GetSection("InfluxDbOptions"));
        services.Configure<EntsoeApiOptions>(hostBuilderContext.Configuration.GetSection("EntsoeApiOptions"));

        // Add collector services.
        services.AddSvKEnergyCollectorService();
        services.AddEntsoeCollectorService();

        // Add Hangfire services.
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
        );

        // Add the processing server as IHostedService
        services.AddHangfireServer();

        // Bind RecurringJobs option.
        services.Configure<ScheduledJobsOptions>(hostBuilderContext.Configuration.GetSection("ScheduledJobs"));

        // Register service that configures recurring jobs.
        services.AddTransient<IRecurringJobsService, RecurringJobsService>();
    })
    .Build();

// Get IRecurringJobsService to configure recurring jobs.
var recurringJobsService = host.Services.GetService<IRecurringJobsService>();
if (recurringJobsService == null)
{
    Console.Error.WriteLine("Could not get service IRecurringJobsService.");
    return;
}

recurringJobsService.ConfigureRecurringJobs();

await host.RunAsync();
