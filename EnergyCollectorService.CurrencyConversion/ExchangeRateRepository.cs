namespace EnergyCollectorService.CurrencyConversion;

using System.Net;

using EnergyCollectorService.CurrencyConversion.RiksbankenApi;
using EnergyCollectorService.CurrencyConversion.RiksbankenApi.Models;

using Microsoft.Extensions.Logging;

using Refit;

public class ExchangeRateRepository : IExchangeRateRepository
{
    #region Fields

    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Dictionary<DateTime, decimal> _exchangeRateCache = new();
    private readonly ILogger<ExchangeRateRepository> _logger;
    private readonly IRiksbankenApiClient _riksbankenApiClient;

    private DateTime? _firstExchangeRateDate;
    private DateTime? _lastExchangeRateDate;

    #endregion

    #region Constructors and Destructors

    public ExchangeRateRepository(ILogger<ExchangeRateRepository> logger, IRiksbankenApiClient riksbankenApiClient)
    {
        _logger = logger;
        _riksbankenApiClient = riksbankenApiClient;
    }

    #endregion

    #region Public Methods and Operators

    public async Task<decimal> Convert(DateTime date, bool loadData)
    {
        await _cacheLock.WaitAsync();

        try
        {
            if (date >= _firstExchangeRateDate && date <= _lastExchangeRateDate)
            {
                // We have already loaded the exchange-rate. Return from cache.
                return _exchangeRateCache[date];
            }

            if (loadData)
            {
                await LoadExchangeRates(date);
            }

            if (_firstExchangeRateDate > date)
            {
                throw new Exception($"No exchange rate available for {date}");
            }

            // Find closest previous exchangerate.
            var counter = 0;
            var currentDate = date;
            while (counter < 10)
            {
                if (_exchangeRateCache.TryGetValue(currentDate, out var exchangeRate))
                {
                    return exchangeRate;
                }

                currentDate = currentDate.AddDays(-1);
                counter++;
            }

            throw new Exception($"No exchange rate available for {date}");
        }

        finally
        {
            _cacheLock.Release();
        }
    }

    #endregion

    #region Methods

    private async Task<IEnumerable<DateAndValue>> GetExchangeRates(DateTime targetFrom, DateTime targetTo)
    {
        var currentFrom = targetFrom;
        var currentTo = targetTo;
        DateTime? loadedFrom = null, loadedTo = null;
        var result = new List<DateAndValue>();
        var retryCounter = 0;
        var noContent = false;
        while (loadedFrom == null || loadedFrom.Value > targetFrom || loadedTo == null || loadedTo.Value < targetTo)
        {
            if (retryCounter >= 2)
            {
                // We've exceeded max retries.
                break;
            }

            var isRetry = false;
            if (noContent || loadedFrom > targetFrom)
            {
                // We've already loaded data, but the targetFrom date was not in the result.
                currentFrom = currentFrom.AddDays(-7);
                isRetry = true;
            }

            if (noContent || (loadedTo < targetTo && currentTo < DateTime.Now.Date))
            {
                // We've already loaded data, but the targetTo date was not in the result.
                currentTo = currentTo.AddDays(7);
                isRetry = true;
            }

            if (isRetry)
            {
                retryCounter++;
            }

            try
            {
                noContent = false;
                var exchangeRates = await _riksbankenApiClient.EurSekCrossrates(currentFrom, currentTo);
                foreach (var exchangeRate in exchangeRates)
                {
                    var addItem = false;
                    if (loadedFrom == null || exchangeRate.Date < loadedFrom)
                    {
                        loadedFrom = exchangeRate.Date;
                        addItem = true;
                    }

                    if (loadedTo == null || exchangeRate.Date > loadedTo)
                    {
                        loadedTo = exchangeRate.Date;
                        addItem = true;
                    }

                    if (addItem)
                    {
                        result.Add(exchangeRate);
                    }
                }

                if (loadedTo < targetTo && currentTo >= DateTime.Now.Date)
                {
                    // We've loaded all available data up until now, and targetTo will not be available even if we expand the search period.
                    targetTo = loadedTo.Value;
                }
            }

            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NoContent)
            {
                // There were no exchange-rates for the specified period. Retry with a longer duration.
                noContent = true;
            }
        }

        return result;
    }

    private async Task LoadExchangeRates(DateTime date)
    {
        var now = DateTime.Now.Date;

        // Load new exchange-rates.
        DateTime fromDate, toDate;
        if (_firstExchangeRateDate == null)
        {
            // There is no data loaded yet.
            fromDate = (date > now ? now : date);
            toDate = now > date ? now : date;
        }
        else if (date < _firstExchangeRateDate)
        {
            // Load data before first rate.
            fromDate = date.AddDays(-7);
            toDate = _firstExchangeRateDate.Value;
        }
        else if (date > _lastExchangeRateDate)
        {
            // Load data after last rate.
            fromDate = _lastExchangeRateDate.Value;
            toDate = date;
        }
        else
        {
            // Should not happen since first and last date is mutually not null.
            throw new Exception("Could not infer which rates to download.");
        }

        var exchangeRates = await GetExchangeRates(fromDate, toDate);
        var sortedExchangeRates = exchangeRates.OrderBy(fx => fx.Date);

        var currentDateTime = fromDate;
        DateAndValue? lastExchangeRate = null;
        foreach (var exchangeRate in sortedExchangeRates)
        {
            if (exchangeRate.Date < currentDateTime)
            {
                // Adjust currentDateTime.
                currentDateTime = exchangeRate.Date;
            }

            while (currentDateTime < exchangeRate.Date)
            {
                // Returned exchange-rates did not start at requested date.
                // Use the last or next rate until we catch up.
                SaveExchangeRate(currentDateTime, (lastExchangeRate ?? exchangeRate).Value);
                currentDateTime = currentDateTime.AddDays(1);
            }

            SaveExchangeRate(currentDateTime, exchangeRate.Value);
            currentDateTime = currentDateTime.AddDays(1);
            lastExchangeRate = exchangeRate;
        }
    }

    private void SaveExchangeRate(DateTime dateTime, decimal rate)
    {
        if (_firstExchangeRateDate == null || dateTime < _firstExchangeRateDate)
        {
            _firstExchangeRateDate = dateTime;
        }

        if (_lastExchangeRateDate == null || dateTime > _lastExchangeRateDate)
        {
            _lastExchangeRateDate = dateTime;
        }

        _exchangeRateCache[dateTime] = rate;
    }

    #endregion
}
