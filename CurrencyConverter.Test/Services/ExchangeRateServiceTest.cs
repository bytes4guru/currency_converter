using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CurrencyConverter.Services;
using CurrencyConverter.Porviders;
using CurrencyConverter.DTOs;
using CurrencyConverter.Configurations;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Options;


namespace CurrencyConverter.Tests.Services
{
    public class ExchangeRateServiceTests
    {
        private readonly Mock<IExchangeRateProviderFactory> _providerFactoryMock;
        private readonly Mock<IExchangeRateProvider> _providerMock;
        private readonly Mock<ILogger<IExchangeRateService>> _loggerMock;
        private readonly ExchangeRateService _service;

        public ExchangeRateServiceTests()
        {
            _providerFactoryMock = new Mock<IExchangeRateProviderFactory>();
            _providerMock = new Mock<IExchangeRateProvider>();
            _loggerMock = new Mock<ILogger<IExchangeRateService>>();

            // Inject mocked provider
            _providerFactoryMock.Setup(x => x.GetProvider(It.IsAny<string>()))
                .Returns(_providerMock.Object);

            var options = Options.Create(new ExcludedCurrenciesSettings
            {
                Exclusions = new[] { "TRY", "PLN", "THB", "MXN" }
            });

            _service = new ExchangeRateService(options, _loggerMock.Object, _providerFactoryMock.Object);
        }

        [Fact]
        public async Task GetLatestRatesAsync_ShouldCallProviderAndReturnRates()
        {
            // Arrange
            var requestDto = new GetLatestRateRequestDto { Base = "USD", Provider = "frankfurter" };
            var expectedResponse = new LatestExchangeRateResponseDto
            {
                Base = "USD",
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
            };

            _providerMock.Setup(p => p.GetLatestRatesAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetLatestRatesAsync(requestDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Base, result.Base);
            Assert.Contains("EUR", result.Rates.Keys);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldThrowException_WhenCurrencyIsExcluded()
        {
            // Arrange
            var requestDto = new ConvertCurrencyRequestDto { From = "TRY", To = "EUR", Amount = 100, Provider = "frankfurter" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.ConvertCurrencyAsync(requestDto));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldCallProviderAndReturnConversion()
        {
            // Arrange
            var requestDto = new ConvertCurrencyRequestDto { From = "USD", To = "EUR", Amount = 100, Provider = "frankfurter" };
            var expectedResponse = new ConvertCurrencyResponseDto
            {
                Base = "USD",
                Amount = 100,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>() { { "EUR", 120 } }
            };

            _providerMock.Setup(p => p.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.ConvertCurrencyAsync(requestDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ShouldCallProviderAndReturnHistoricalRates()
        {
            // Arrange
            var requestDto = new HistoricalRatesRequestDto
            {
                BaseCurrency = "USD",
                Start = DateTime.UtcNow.AddDays(-30),
                End = DateTime.UtcNow,
                Page = 1,
                PageSize = 10,
                Provider = "frankfurter"
            };

            var expectedResponse = new HistoricalRatesResponseDto
            {
                Base = "USD",
                Start_Date = DateTime.UtcNow.AddDays(-30),
                End_Date = DateTime.UtcNow,
                Rates = new Dictionary<string, Dictionary<string, decimal>>
{
                    { "2025-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } }
                },
                Amount = 100m,
                TotalRecords = 10
            };

            _providerMock.Setup(p => p.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetHistoricalRatesAsync(requestDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Base, result.Base);
            Assert.Contains("EUR", result.Rates.SelectMany(r => r.Value.Keys));
        }
    }
}
