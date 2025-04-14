namespace CurrencyConverter.Services
{
    public interface ICurrencyProviderFactory
    {
        IExchangeRateService GetProvider(string providerName);
    }

    public class CurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly IServiceProvider _services;

        public CurrencyProviderFactory(IServiceProvider services)
        {
            _services = services;
        }

        public IExchangeRateService GetProvider(string providerName)
        {
            // In future, support other providers based on the name
            return _services.GetRequiredService<IExchangeRateService>();
        }
    }
}
