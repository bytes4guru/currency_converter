using Xunit;
using Moq;
using CurrencyConverter;
using CurrencyConverter.Services;
using CurrencyConverter.ViewModels;
using CurrencyConverter.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CurrencyConverter.Tests.Controllers
{
    public class ExchangeRateControllerTests
    {
        private readonly Mock<IExchangeRateService> _mockService;
        private readonly ExchangeRateController _controller;

        public ExchangeRateControllerTests()
        {
            _mockService = new Mock<IExchangeRateService>();
            _controller = new ExchangeRateController(_mockService.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsOkResult_WithRates()
        {
            var mockRates = new ExchangeRateResponse()
            {
                Base="EUR",
                Date=DateTime.Now,
                Rates= new Dictionary<string, decimal>() { { "USD", 1.0m }, { "EUR", 0.85m } }
            };
            _mockService.Setup(s => s.GetLatestRatesAsync("USD")).ReturnsAsync(mockRates);

            var result = await _controller.GetLatestRates("USD");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(mockRates, okResult.Value);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsBadRequest_OnException()
        {
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>()))
                        .ThrowsAsync(new Exception("Something went wrong"));

            var result = await _controller.GetLatestRates("USD");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Something went wrong", badRequest.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsOkResult_WithAmount()
        {
            var convertedAmount = new ConvertCurrencyResponse() {
                Amount = 12.1F,
                Base = "USD",
                Date = DateTime.Now,
                Rates = new Dictionary<string, decimal>()
            };
            _mockService.Setup(s => s.ConvertCurrencyAsync("USD", "EUR", 100))
                        .ReturnsAsync(convertedAmount);

            var result = await _controller.ConvertCurrency("USD", "EUR", 100);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(convertedAmount, okResult.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsBadRequest_OnArgumentException()
        {
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                        .ThrowsAsync(new ArgumentException("Invalid currency"));

            var result = await _controller.ConvertCurrency("XXX", "EUR", 100);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid currency", badRequest.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsProblem_OnGeneralException()
        {
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                        .ThrowsAsync(new Exception("Server error"));

            var result = await _controller.ConvertCurrency("USD", "EUR", 100);

            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, problem.StatusCode);
        }

        //[Fact]
        //public async Task GetHistoricalRates_ReturnsOkResult_WithData()
        //{
        //    var history = new List<ExchangeRateViewModel>
        //    {
        //        new ExchangeRateViewModel { Date = DateTime.Today, Rates = new Dictionary<string, decimal> { { "EUR", 0.9m } } }
        //    };

        //    _mockService.Setup(s => s.GetHistoricalRatesAsync("USD", It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 10))
        //                .ReturnsAsync(history);

        //    var result = await _controller.GetHistoricalRates("USD", DateTime.Today.AddDays(-2), DateTime.Today, 1, 10);

        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    Assert.Equal(history, okResult.Value);
        //}

        [Fact]
        public async Task GetHistoricalRates_ReturnsProblem_OnApiException()
        {
            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
                        .ThrowsAsync(new ExchangeRateApiException("API error"));

            var result = await _controller.GetHistoricalRates("USD", DateTime.Today.AddDays(-5), DateTime.Today, 1, 10);

            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, problem.StatusCode);
        }

        [Fact]
        public async Task GetHistoricalRates_ReturnsBadRequest_OnArgumentException()
        {
            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>()))
                        .ThrowsAsync(new ArgumentException("Invalid date range"));

            var result = await _controller.GetHistoricalRates("USD", DateTime.Today, DateTime.Today.AddDays(-1), 1, 10);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid date range", badRequest.Value);
        }
    }
}
