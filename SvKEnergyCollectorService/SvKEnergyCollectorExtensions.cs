namespace SvKEnergyCollectorService;

using Microsoft.Extensions.DependencyInjection;

public static class SvKEnergyCollectorExtensions
{
    #region Public Methods and Operators

    public static IServiceCollection AddSvKEnergyCollectorService(this IServiceCollection services)
    {
        // Add SvKEnergyCollectorService service.
        return services.AddTransient<ISvKEnergyCollectorService, SvKEnergyCollectorService>();
    }

    #endregion
}
