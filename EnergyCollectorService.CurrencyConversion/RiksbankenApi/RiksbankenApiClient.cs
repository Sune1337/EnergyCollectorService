namespace EnergyCollectorService.CurrencyConversion.RiksbankenApi;

using EnergyCollectorService.CurrencyConversion.RiksbankenApi.Models;

using Refit;

public interface IRiksbankenApiClient
{
    #region Public Methods and Operators

    [Get("/CrossRates/SEKEURPMI/SEK/{from}/{to}")]
    public Task<DateAndValue[]> EurSekCrossrates(DateTime from, DateTime to);

    #endregion
}
