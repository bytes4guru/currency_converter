using Microsoft.Extensions.Caching.Memory;
using CurrencyConverter.Exceptions;
using CurrencyConverter.Helpers;

using CurrencyConverter.ViewModels;

namespace CurrencyConverter.Services
{
    public class FrankfurterExchangeRateService : IExchangeRateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IExchangeRateService> _logger;
        private readonly string[] _excludedCurrencies;

        public FrankfurterExchangeRateService(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration configuration, ILogger<IExchangeRateService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _excludedCurrencies = configuration.GetSection("ExcludedCurrencies").Get<string[]>() ?? Array.Empty<string>();
            if (_excludedCurrencies.Length == 0)
            {
                _logger.LogWarning("ExcludedCurrencies is not defined in the configuration. Using empty list.");
            }
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            baseCurrency = baseCurrency.ToUpper();
            var today = DateTime.UtcNow.ToString("yy-MM-dd");
            var cacheKey = $"{today}_{baseCurrency}";
            if (_cache.TryGetValue(cacheKey, out ExchangeRateResponse? cached))
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

            var client = _httpClientFactory.CreateClient("Frankfurter");
            var response = await client.GetAndDeserializeAsync<ExchangeRateResponse>($"/latest?base={baseCurrency}", _logger);
            
            _cache.Set(cacheKey, response, TimeSpan.FromDays(1));
            return response!;
        }

        public async Task<ConvertCurrencyResponse> ConvertCurrencyAsync(string from, string to, decimal amount)
        {
            from = from.ToUpper();
            to = to.ToUpper();
            if (_excludedCurrencies.Contains(from) || _excludedCurrencies.Contains(to))
                throw new ArgumentException("One or more currencies are restricted.");

            var client = _httpClientFactory.CreateClient("Frankfurter");
            var response = await client.GetAndDeserializeAsync<ConvertCurrencyResponse>($"/latest?amount={amount}&from={from}&to={to}", _logger);
            return response!;
        }

        public async Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize)
        {
            baseCurrency = baseCurrency.ToUpper();
            var cacheKey = $"historical_{baseCurrency}_{start:yyyyMMdd}_{end:yyyyMMdd}";

            if (!_cache.TryGetValue(cacheKey, out HistoricalRatesResponse? fullResponse) || fullResponse?.Rates == null)
            {
                var client = _httpClientFactory.CreateClient("Frankfurter");
                var requestUrl = $"/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?base={baseCurrency}";

                try
                {
                    fullResponse = await client.GetAndDeserializeAsync<HistoricalRatesResponse>(requestUrl, _logger);
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

            // Ensure ordered pagination by date key
            var pagedRates = fullResponse.Rates
                .OrderBy(kv => kv.Key)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return new HistoricalRatesResponse
            {
                Base = fullResponse.Base,
                Start_Date = start,
                End_Date = end,
                Rates = pagedRates,
                TotalRecords = fullResponse.Rates.Count
            };
        }
    }
}
