using Microsoft.Extensions.Caching.Memory;
using CurrencyConverter.Exceptions;
using CurrencyConverter.Extensions;

using CurrencyConverter.DTOs;
using CurrencyConverter.Porviders;
using CurrencyConverter.Configurations;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly ILogger<IExchangeRateService> _logger;
        private readonly string[] _excludedCurrencies;
        private readonly IExchangeRateProviderFactory _providerFactory;
        

        public ExchangeRateService(
            IOptions<ExcludedCurrenciesSettings> settings,
            ILogger<IExchangeRateService> logger,
            IExchangeRateProviderFactory providerFactory)
        {
            _logger = logger;
            _providerFactory = providerFactory;

            _excludedCurrencies = settings.Value.Exclusions;
            if (_excludedCurrencies.Length == 0)
            {
                _logger.LogWarning("ExcludedCurrencies is not defined in the configuration. Using empty list.");
            }
        }

        public async Task<LatestExchangeRateResponseDto> GetLatestRatesAsync(GetLatestRateRequestDto dto)
        {
            var exchangeRateProvider = _providerFactory.GetProvider(dto.Provider);
            var response = await exchangeRateProvider.GetLatestRatesAsync(dto.Base);
            return response!;
        }

        public async Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(ConvertCurrencyRequestDto dto)
        {
            if (_excludedCurrencies.Contains(dto.From.ToUpper()) || _excludedCurrencies.Contains(dto.To.ToUpper()))
                throw new ArgumentException("One or more currencies are restricted.");
            var exchangeRateProvider = _providerFactory.GetProvider(dto.Provider);
            var response = await exchangeRateProvider.ConvertCurrencyAsync(dto.From,dto.To, dto.Amount);
            return response!;
        }

        public async Task<HistoricalRatesResponseDto> GetHistoricalRatesAsync(HistoricalRatesRequestDto dto)
        {
            var exchangeRateProvider = _providerFactory.GetProvider(dto.Provider);
            var fullResponse = await exchangeRateProvider.GetHistoricalRatesAsync(dto.BaseCurrency, dto.Start, dto.End, dto.Page, dto.PageSize);

            return fullResponse;
        }
    }
}
