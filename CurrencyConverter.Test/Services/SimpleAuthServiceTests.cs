using CurrencyConverter.Models;
using CurrencyConverter.Services;
using CurrencyConverter.DTOs;
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
            var loginRequestDto = new LoginRequestDto() { Username = "admin", Password = "1234" };

            // Act
            var token = _authService.Authenticate(loginRequestDto);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void Authenticate_InvalidCredentials_ThrowsException()
        {
            // Arrange
            var loginRequestDto = new LoginRequestDto() { Username = "admin", Password = "wrong password" };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _authService.Authenticate(loginRequestDto));
            Assert.Equal("username or pawword not correct", ex.Message);
        }

        [Fact]
        public void Authenticate_GeneratesJwt_WithCorrectClaims()
        {
            // Arrange
            var loginRequestDto = new LoginRequestDto() { Username = "user", Password = "1234" };

            // Act
            var token = _authService.Authenticate(loginRequestDto);

            // Decode token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "user");
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
        }
    }
}
