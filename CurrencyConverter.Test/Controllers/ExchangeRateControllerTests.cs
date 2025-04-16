using Xunit;
using Moq;
using CurrencyConverter;
using CurrencyConverter.Services;
using CurrencyConverter.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CurrencyConverter.DTOs;

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
            var mockRates = new LatestExchangeRateResponseDto()
            {
                Base="EUR",
                Date=DateTime.Now,
                Rates= new Dictionary<string, decimal>() { { "USD", 1.0m }, { "EUR", 0.85m } }
            };
            var dto = new GetLatestRateRequestDto() { Base = "EUR" };
            _mockService.Setup(s => s.GetLatestRatesAsync(dto)).ReturnsAsync(mockRates);

            var result = await _controller.GetLatestRates(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(mockRates, okResult.Value);
        }

        [Fact]
        public async Task GetLatestRates_ReturnsBadRequest_OnException()
        {
            _mockService.Setup(s => s.GetLatestRatesAsync(It.IsAny<GetLatestRateRequestDto>()))
                        .ThrowsAsync(new Exception("Something went wrong"));
            var dto = new GetLatestRateRequestDto() { Base = "EUR" };
            var result = await _controller.GetLatestRates(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Something went wrong", badRequest.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsOkResult_WithAmount()
        {
            var convertedAmount = new ConvertCurrencyResponseDto() {
                Amount = 12.1F,
                Base = "USD",
                Date = DateTime.Now,
                Rates = new Dictionary<string, decimal>()
            };
            var requestDto = new ConvertCurrencyRequestDto() { From="USD", To="EUR", Amount=100 };

            _mockService.Setup(s => s.ConvertCurrencyAsync(requestDto))
                        .ReturnsAsync(convertedAmount);

            var result = await _controller.ConvertCurrency(requestDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(convertedAmount, okResult.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsBadRequest_OnArgumentException()
        {
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequestDto>()))
                        .ThrowsAsync(new ArgumentException("Invalid currency"));
            var requestDto = new ConvertCurrencyRequestDto() { From="XXX", To= "USD",  Amount = 100 };
            var result = await _controller.ConvertCurrency(requestDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid currency", badRequest.Value);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsProblem_OnGeneralException()
        {
            
            _mockService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<ConvertCurrencyRequestDto>()))
                        .ThrowsAsync(new Exception("Server error"));

            var requestDto = new ConvertCurrencyRequestDto() { From = "EUR", To = "USD", Amount = 100 };
            var result = await _controller.ConvertCurrency(requestDto);

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
            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<HistoricalRatesRequestDto>()))
                        .ThrowsAsync(new ExchangeRateApiException("API error"));
            var dto = new HistoricalRatesRequestDto() { BaseCurrency = "USD", Start = DateTime.Today.AddDays(-10), End = DateTime.Today, Page = 1, PageSize = 10 };
            var result = await _controller.GetHistoricalRates(dto);

            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, problem.StatusCode);
        }

        [Fact]
        public async Task GetHistoricalRates_ReturnsBadRequest_OnArgumentException()
        {
            _mockService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<HistoricalRatesRequestDto>()))
                        .ThrowsAsync(new ArgumentException("Invalid date range"));
            var dto = new HistoricalRatesRequestDto() { BaseCurrency="USD", Start = DateTime.Today.AddDays(-10), End= DateTime.Today, Page=1, PageSize=10 };
            var result = await _controller.GetHistoricalRates(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid date range", badRequest.Value);
        }
    }
}
