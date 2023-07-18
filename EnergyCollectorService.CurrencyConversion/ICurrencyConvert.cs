namespace EnergyCollectorService.CurrencyConversion;

public interface ICurrencyConvert
{
    #region Public Methods and Operators

    public Task<decimal> Convert(DateTime date);

    #endregion
}
