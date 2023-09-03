using EntsoeCollectorService.Configuration;
using EntsoeCollectorService.EntsoeApi;
using EntsoeCollectorService.EntsoeApi.Serialization;
using EntsoeCollectorService.Measurements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace EntsoeCollectorService;

public static class EntsoeCollectorExtensions
{
    #region Public Methods and Operators

    public static IServiceCollection AddEntsoeCollectorService(this IServiceCollection services)
    {
        // Register a HTTP client we will use for invoking Entsoe API.
        // We specifically declare a custom HttpClient so that we can add authentication HTTP headers to all requests automatically.
        services.AddHttpClient("EntsoeApiHttpClient", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetService<IOptions<EntsoeApiOptions>>();
            if (options?.Value == null)
            {
                throw new Exception("EntsoeApiOptions is null.");
            }

            if (string.IsNullOrEmpty(options.Value.BaseUrl))
            {
                throw new Exception("EntsoeApiOptions.BaseUrl is null or empty.");
            }

            client.BaseAddress = new Uri(options.Value.BaseUrl);
        });

        // Add EntsoeApi client.
        services.AddRefitClient<IEntsoeApiClient>(serviceProvider => new RefitSettings
        {
            ContentSerializer = new XmlContentSerializer(),
            UrlParameterFormatter = new UrlParameterFormatter()
        }, httpClientName: "EntsoeApiHttpClient");

        // Add data-transfer helpers.
        services.AddTransient<GenerateMeasurements>();
        services.AddTransient<LoadMeasurements>();
        services.AddTransient<DayAheadPriceMeasurements>();
        services.AddTransient<PhysicalFlowMeasurements>();

        // Add EntsoeCollectorService service.
        services.AddTransient<IEntsoeCollectorService, EntsoeCollectorService>();

        return services;
    }

    #endregion
}
