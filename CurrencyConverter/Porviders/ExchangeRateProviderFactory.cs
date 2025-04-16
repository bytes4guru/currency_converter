using CurrencyConverter.Configurations;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace CurrencyConverter.Porviders
{
    public interface IExchangeRateProviderFactory {
        IExchangeRateProvider GetProvider(string? providerName);
    }
    public class ExchangeRateProviderFactory :IExchangeRateProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ExchangeRateApiSettings _settings;

        public ExchangeRateProviderFactory(IServiceProvider serviceProvider, IOptions<ExchangeRateApiSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        public IExchangeRateProvider GetProvider(string? providerName)
        {
            providerName = providerName ?? _settings.Default;

            var providerConfig = _settings.Providers.FirstOrDefault(p =>
                p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (providerConfig == null)
            {
                throw new ArgumentException($"Currency provider '{providerName}' is not configured.");
            }
            return providerName.ToLower() switch
            {
                "frankfurter" => _serviceProvider.GetRequiredService<FrankfurterProvider>(),
                _ => throw new NotSupportedException($"Currency provider '{providerName}' is not supported.")
            };
        }
    }
}
