using CurrencyConverter.ViewModels;
public interface IExchangeRateService
{
    Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
    Task<ConvertCurrencyResponse> ConvertCurrencyAsync(string from, string to, decimal amount);
    Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize);
}


