using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CurrencyConverter.Exceptions;
using CurrencyConverter.Extensions;
using Moq.Protected;

namespace CurrencyConverter.Tests
{
    public class HttpClientExtensionsTests
    {
        private readonly Mock<ILogger> _loggerMock;

        public HttpClientExtensionsTests()
        {
            _loggerMock = new Mock<ILogger>();
        }

        // 1. Successful Response Test
        [Fact]
        public async Task GetAndDeserializeAsync_Should_Deserialize_Content_When_Successful()
        {

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"Key\": \"value\"}")
                });

            var client = new HttpClient(handlerMock.Object); // Use the mocked handler
            var url = "https://example.com/api/data"; // Mocked URL

            // Act
            var result = await client.GetAndDeserializeAsync<TestResponse>(url, _loggerMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("value", result.Key);
        }

        // 2. HTTP Request Failure Test
        [Fact]
        public async Task GetAndDeserializeAsync_Should_Throw_Exception_When_Request_Fails()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(); // Mock the handler
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Request failed")); // Simulate a failure

            var client = new HttpClient(handlerMock.Object); // Use the mocked handler
            var url = "https://example.com/api/data"; // Mocked URL
            var expectedExceptionMessage = "Failed to reach API";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExchangeRateApiException>(() =>
                client.GetAndDeserializeAsync<object>(url, _loggerMock.Object));

            Assert.Contains(expectedExceptionMessage, exception.Message); // Assert the exception message
        }


        // 3. Deserialization Failure Test
        [Fact]
        public async Task GetAndDeserializeAsync_Should_Throw_Exception_When_Deserialization_Fails()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(); // Mock the handler
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Invalid JSON Content") // Invalid JSON content
            };

            // Set up the handler to return the mocked response
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(mockResponse);

            var client = new HttpClient(handlerMock.Object); // Use the mocked handler
            var url = "https://example.com/api/data"; // Mocked URL
            var expectedErrorMessage = "Failed to parse JSON";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExchangeRateApiException>(() =>
                client.GetAndDeserializeAsync<TestResponse>(url, _loggerMock.Object));

            // Assert that the exception message contains the expected error
            Assert.Contains(expectedErrorMessage, exception.Message); // Verify the exception message
        }


        // 4. API Error Message in Response Test
        [Fact]
        public async Task GetAndDeserializeAsync_Should_Throw_Exception_When_API_Returns_Error_Message()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(); // Mock the handler
            var mockErrorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"Error occurred\"}")
            };

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(mockErrorResponse); // Return the mocked error response

            var client = new HttpClient(handlerMock.Object); // Use the mocked handler
            var url = "https://example.com/api/data"; // Mocked URL
            var expectedErrorMessage = "API failed with HTTP BadRequest";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExchangeRateApiException>(() =>
                client.GetAndDeserializeAsync<object>(url, _loggerMock.Object));

            Assert.Contains(expectedErrorMessage, exception.Message); // Assert the exception message
        }

    }

    // Custom Fake HTTP Message Handler for mocking HttpClient responses
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Key\":\"value\"}") // Mock response content
            };
            return Task.FromResult(response);
        }
    }

    public class TestResponse
    {
        public required string Key { get; set; }
    }
}
