namespace CurrencyConverter.Exceptions
{
    public class ExchangeRateApiException : Exception
    {
        public ExchangeRateApiException(string message):base(message) {}
    }
}
