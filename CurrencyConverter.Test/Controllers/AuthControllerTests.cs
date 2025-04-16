using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.Services;
using CurrencyConverter;
using CurrencyConverter.DTOs;

public class AuthControllerTests
{
    private readonly AuthController _controller;
    private readonly Mock<IAuthService> _mockAuthService;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    [Fact]
    public void Login_ReturnsOk_WithToken()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Username = "admin", Password = "1234" };
        var expectedToken = "mock-jwt-token";

        _mockAuthService
            .Setup(s => s.Authenticate(loginRequest))
            .Returns(expectedToken);

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsType<LoginResponseDto>(okResult.Value);  // Assert as LoginResponse type
        Assert.Equal(expectedToken, value.Token);  // Assert that the returned token matches
    }

    [Fact]
    public void Login_ReturnsBadRequest_WithInvalidCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Username = "invalid", Password = "wrongpassword" };

        _mockAuthService
            .Setup(s => s.Authenticate(loginRequest))
            .Throws(new ArgumentException("Invalid credentials"));

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.StartsWith("Invalid credentials", badRequestObjectResult.Value?.ToString()); // Ensure the error message is as expected
    }

   
}
