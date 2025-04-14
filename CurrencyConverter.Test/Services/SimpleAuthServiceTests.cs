using CurrencyConverter.Models;
using CurrencyConverter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace CurrencyConverter.Test.Services
{
    public class SimpleAuthServiceTests
    {
        private readonly SimpleAuthService _authService;
        private readonly SymmetricSecurityKey _securityKey;

        public SimpleAuthServiceTests()
        {
            var loggerMock = new Mock<ILogger<IAuthService>>();
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASecretKeyForJwt123456kThisIsASecretKeyForJwt123456kThisIsASecretKeyForJwt123456kThisIsASecretKeyForJwt123456kThisIsASecretKeyForJwt123456k"));
            _authService = new SimpleAuthService(_securityKey, loggerMock.Object);
        }

        [Fact]
        public void Authenticate_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var username = "admin";
            var password = "1234";

            // Act
            var token = _authService.Authenticate(username, password);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void Authenticate_InvalidCredentials_ThrowsException()
        {
            // Arrange
            var username = "admin";
            var password = "wrongpassword";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _authService.Authenticate(username, password));
            Assert.Equal("username or pawword not correct", ex.Message);
        }

        [Fact]
        public void Authenticate_GeneratesJwt_WithCorrectClaims()
        {
            // Arrange
            var username = "user";
            var password = "1234";

            // Act
            var token = _authService.Authenticate(username, password);

            // Decode token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "user");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
        }
    }
}
