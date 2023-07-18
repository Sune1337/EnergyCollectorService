namespace EnergyCollectorService.CurrencyConversion;

public interface IExchangeRateRepository
{
    #region Public Methods and Operators

    public Task<decimal> Convert(DateTime date, bool loadData);

    #endregion
}
