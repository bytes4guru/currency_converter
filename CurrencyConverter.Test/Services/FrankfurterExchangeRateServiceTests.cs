using CurrencyConverter.Services;
using CurrencyConverter.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Test.Services
{
    public class FrankfurterExchangeRateServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<IExchangeRateService>> _loggerMock;

        public FrankfurterExchangeRateServiceTests()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<IExchangeRateService>>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"ExcludedCurrencies:0", "TRY"},
                {"ExcludedCurrencies:1", "PLN"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task GetLatestRatesAsync_ShouldReturnFromCache_IfAvailable()
        {
            var service = CreateService();

            var baseCurrency = "USD";
            var today = DateTime.UtcNow.ToString("yy-MM-dd");
            var cacheKey = $"{today}_{baseCurrency}";

            var cached = new ExchangeRateResponse { Base = baseCurrency };
            _cache.Set(cacheKey, cached);

            var result = await service.GetLatestRatesAsync(baseCurrency);

            Assert.Equal("USD", result.Base);
        }

        [Fact]
        public async Task GetLatestRatesAsync_ShouldFetchAndCache_WhenNotInCache()
        {
            var baseCurrency = "EUR";
            var expected = new ExchangeRateResponse { Base = baseCurrency };
            var json = JsonSerializer.Serialize(expected);

            var handler = new FakeHttpMessageHandler(json);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };

            _httpClientFactoryMock.Setup(f => f.CreateClient("Frankfurter")).Returns(client);

            var service = CreateService();

            var result = await service.GetLatestRatesAsync(baseCurrency);

            Assert.Equal("EUR", result.Base);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldThrowException_ForExcludedCurrency()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ConvertCurrencyAsync("TRY", "USD", 100));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ShouldReturnResult_ForValidCurrencies()
        {
            var json = JsonSerializer.Serialize(new ConvertCurrencyResponse
            {
                Amount = 100,
                Base = "USD",
                Date = DateTime.Now,
                Rates = new Dictionary<string, decimal> { { "EUR", 91.23M } }
            });

            var handler = new FakeHttpMessageHandler(json);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };

            _httpClientFactoryMock.Setup(f => f.CreateClient("Frankfurter")).Returns(client);

            var service = CreateService();

            var result = await service.ConvertCurrencyAsync("USD", "EUR", 100);

            Assert.Equal(91.23M, result.Rates["EUR"]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ShouldReturnPaginatedResults()
        {
            var baseCurrency = "EUR";
            var start = new DateTime(2020, 1, 1);
            var end = new DateTime(2020, 1, 5);

            var rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["2020-01-01T00:00:00"] = new() { ["USD"] = 1.1M },
                ["2020-01-02T00:00:00"] = new() { ["USD"] = 1.2M },
                ["2020-01-03T00:00:00"] = new() { ["USD"] = 1.3M },
                ["2020-01-04T00:00:00"] = new() { ["USD"] = 1.4M },
                ["2020-01-05T00:00:00"] = new() { ["USD"] = 1.5M },
            };

            var json = JsonSerializer.Serialize(new HistoricalRatesResponse
            {
                Base = baseCurrency,
                Rates = rates
            });

            var handler = new FakeHttpMessageHandler(json);
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };

            _httpClientFactoryMock.Setup(f => f.CreateClient("Frankfurter")).Returns(client);

            var service = CreateService();

            var response = await service.GetHistoricalRatesAsync(baseCurrency, start, end, 1, 2);

            Assert.Equal(2, response.Rates.Count);
            Assert.Equal(5, response.TotalRecords);
        }

        private FrankfurterExchangeRateService CreateService()
        {
            return new FrankfurterExchangeRateService(
                _httpClientFactoryMock.Object,
                _cache,
                _configuration,
                _loggerMock.Object);
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseContent;

            public FakeHttpMessageHandler(string responseContent)
            {
                _responseContent = responseContent;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
