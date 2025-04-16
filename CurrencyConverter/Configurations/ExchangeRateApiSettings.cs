namespace CurrencyConverter.Configurations
{
    public class ExchangeRateApiSettings
    {
        public required string Default {  get; set; }
        public required List<ProviderSettings> Providers { get; set; }
    }


    public class ProviderSettings
    {
        public string Name { get; set; } = default!;
        public string BaseUrl { get; set; } = default!;
        public int TimeoutSeconds { get; set; } = 5;
        public int RetryCount { get; set; } = 3;
        public int RetryBackoffSeconds { get; set; } = 2;
        public int CircuitBreakerFailureCount { get; set; }
        public int CircuitBreakerDurationSeconds { get; set; }
    }


}
