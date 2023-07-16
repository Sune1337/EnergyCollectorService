namespace SvKEnergyCollectorService;

using Microsoft.Extensions.DependencyInjection;

public static class SvKEnergyCollectorExtensions
{
    #region Public Methods and Operators

    public static IServiceCollection AddSvKEnergyCollectorService(this IServiceCollection services)
    {
        // Register a HTTP client we will use for invoking SvK API.
        services.AddHttpClient("SvKAPI", client =>
        {
            client.BaseAddress = new Uri("https://www.svk.se/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Add SvKEnergyCollectorService service.
        return services.AddTransient<ISvKEnergyCollectorService, SvKEnergyCollectorService>();
    }

    #endregion
}
