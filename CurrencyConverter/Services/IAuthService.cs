namespace CurrencyConverter.Services
{
    public interface IAuthService
    {
        public string Authenticate(string userName, string password);
    }
}
