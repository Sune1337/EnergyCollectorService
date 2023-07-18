namespace EnergyCollectorService.CurrencyConversion;

public class CurrencyConvert : ICurrencyConvert
{
    #region Fields

    private readonly HashSet<DateTime> _attemtedDates = new();
    private readonly IExchangeRateRepository _exchangeRateRepository;

    #endregion

    #region Constructors and Destructors

    public CurrencyConvert(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    #endregion

    #region Public Methods and Operators

    public async Task<decimal> Convert(DateTime date)
    {
        var dateOnly = date.Date;
        var attemptedDate = _attemtedDates.Contains(dateOnly);
        var result = await _exchangeRateRepository.Convert(dateOnly, attemptedDate == false);
        if (!attemptedDate)
        {
            _attemtedDates.Add(dateOnly);
        }

        return result;
    }

    #endregion
}
