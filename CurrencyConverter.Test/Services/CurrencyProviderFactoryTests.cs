using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CurrencyConverter.Services;

namespace CurrencyConverter.Tests.Services
{
    public class CurrencyProviderFactoryTests
    {
        [Fact]
        public void GetProvider_ReturnsExchangeRateServiceInstance()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockExchangeRateService = new Mock<IExchangeRateService>();

            // Set up the mock IServiceProvider to return the mockExchangeRateService
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IExchangeRateService)))
                               .Returns(mockExchangeRateService.Object);

            var factory = new CurrencyProviderFactory(mockServiceProvider.Object);

            // Act
            var provider = factory.GetProvider("SomeProviderName");

            // Assert
            Assert.NotNull(provider);
            Assert.IsAssignableFrom<IExchangeRateService>(provider);
            Assert.Same(mockExchangeRateService.Object, provider);
        }
    }
}
