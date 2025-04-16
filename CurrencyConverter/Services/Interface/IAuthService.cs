using CurrencyConverter.DTOs;

namespace CurrencyConverter.Services
{
    public interface IAuthService
    {
        public string Authenticate(LoginRequestDto credentials);
    }
}
