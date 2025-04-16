using CurrencyConverter.DTOs;

namespace CurrencyConverter.Porviders
{
    public interface IExchangeRateProvider
    {
        public Task<LatestExchangeRateResponseDto> GetLatestRatesAsync(string baseCurrency);
        public Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(string from, string to, decimal amount);
        public Task<HistoricalRatesResponseDto> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize);
    }
}
