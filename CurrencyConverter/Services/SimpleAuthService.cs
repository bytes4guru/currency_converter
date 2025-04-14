using CurrencyConverter.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CurrencyConverter.Services
{
    public class SimpleAuthService : IAuthService
    {
        private readonly ILogger<IAuthService> _logger;
        private SecurityKey _securityKey;

        private readonly IEnumerable<User> _users = new User[] {
            new User() { Id=1, Name="Admin User", UserName="admin", Password="1234", Role="admin" },
            new User() { Id=2, Name="Normal User", UserName="user", Password="1234", Role="user" },
        }; 
        public SimpleAuthService(SecurityKey securrityKey, ILogger<IAuthService> logger)
        {
            _logger = logger;
            _securityKey = securrityKey;
        }
        public string Authenticate(string username, string password)
        {
            var user = _users.FirstOrDefault<User>(p=>(p.UserName == username && p.Password == password));
            if (user == null) {
                throw new ArgumentException("username or pawword not correct");
            }
            return GenerateJwtToken(username, user.Role);
        }

        private string GenerateJwtToken(string username, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var creds = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(102),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
