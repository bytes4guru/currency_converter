using CurrencyConverter.DTOs;
using CurrencyConverter.Exceptions;
using CurrencyConverter.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;

namespace CurrencyConverter.Porviders
{
    public class FrankfurterProvider : IExchangeRateProvider
    {
       
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IExchangeRateProvider> _logger;

        public FrankfurterProvider(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<IExchangeRateProvider> logger)
        {
            _cache = cache;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("frankfurter");
        }

        public async Task<LatestExchangeRateResponseDto> GetLatestRatesAsync(string baseCurrency)
        {

            baseCurrency = baseCurrency.ToUpper();
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var cacheKey = $"{today}_{baseCurrency}";
            if (_cache.TryGetValue(cacheKey, out LatestExchangeRateResponseDto? cached))
            {
                if (cached == null)
                {
                    _logger.LogWarning("Cache returned null for key: {CacheKey}", cacheKey);
                }
                else
                {
                    _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                    return cached;
                }
            }
            else
            {
                _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
            }

            var response = await _httpClient.GetAndDeserializeAsync<LatestExchangeRateResponseDto>($"/latest?base={baseCurrency}", _logger);

            _cache.Set(cacheKey, response, TimeSpan.FromDays(1));
            return response!;
        }
        public async Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            var response = await _httpClient.GetAndDeserializeAsync<ConvertCurrencyResponseDto>($"/latest?amount={amount}&from={from}&to={to}", _logger);
            return response!;
        }

        public async Task<HistoricalRatesResponseDto> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize)
        {
            baseCurrency = baseCurrency.ToUpper();
            var startStr = start.ToString("yyyy-MM-dd");
            var endStr = end.ToString("yyyy-MM-dd");
            var cacheKey = $"historical_{baseCurrency}_{startStr}_{endStr}";

            if (!_cache.TryGetValue(cacheKey, out HistoricalRatesResponseDto? fullResponse) || fullResponse?.Rates == null)
            {
                var requestUrl = $"/{startStr}..{endStr}?base={baseCurrency}";

                try
                {
                    fullResponse = await _httpClient.GetAndDeserializeAsync<HistoricalRatesResponseDto>(requestUrl, _logger);
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1));

                    _cache.Set(cacheKey, fullResponse, cacheEntryOptions);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error fetching historical rates from Frankfurter API.");
                    throw new ExchangeRateApiException(ex.Message);
                }
            }
            var pagedRates = fullResponse.Rates
               .OrderBy(kv => kv.Key)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToDictionary(kv => kv.Key, kv => kv.Value);


            return new HistoricalRatesResponseDto
            {
                Base = fullResponse.Base,
                Start_Date = start,
                End_Date = end,
                Rates = pagedRates,
                Amount = fullResponse.Amount,
                TotalRecords = fullResponse.Rates.Count
            };

        }
    }
}
