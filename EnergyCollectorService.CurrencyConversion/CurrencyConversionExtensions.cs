namespace EnergyCollectorService.CurrencyConversion;

using EnergyCollectorService.CurrencyConversion.Configuration;
using EnergyCollectorService.CurrencyConversion.RiksbankenApi;
using EnergyCollectorService.CurrencyConversion.RiksbankenApi.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Refit;

public static class CurrencyConversionExtensions
{
    #region Public Methods and Operators

    public static IServiceCollection AddCurrencyConvertService(this IServiceCollection services)
    {
        // Register a HTTP client we will use for invoking Riksbanken API.
        services.AddHttpClient("RiksbankenApiHttpClient", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetService<IOptions<RiksbankenApiOptions>>();
            if (options?.Value == null)
            {
                throw new Exception("RiksbankenApiOptions is null.");
            }

            if (string.IsNullOrEmpty(options.Value.BaseUrl))
            {
                throw new Exception("RiksbankenApiOptions.BaseUrl is null or empty.");
            }

            client.BaseAddress = new Uri(options.Value.BaseUrl);
        });

        // Add RiksbankenApi client.
        services.AddRefitClient<IRiksbankenApiClient>(serviceProvider => new RefitSettings
        {
            UrlParameterFormatter = new UrlParameterFormatter()
        }, httpClientName: "RiksbankenApiHttpClient");

        // Add CurrencyConvert service.
        services.AddSingleton<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddTransient<ICurrencyConvert, CurrencyConvert>();

        return services;
    }

    #endregion
}
