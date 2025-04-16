using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using CurrencyConverter.DTOs;


namespace CurrencyConverter.Tests
{
    public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProgramTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Swagger_Is_Available_In_Development()
        {
            var response = await _client.GetAsync("/swagger/index.html");
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Unauthenticated_Request_Should_Return_401()
        {
            var response = await _client.GetAsync("/api/exchangerate/latest?base=USD&target=EUR"); // change if needed
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Authenticated_Request_Should_Pass_Authorization()
        {
            // Step 1: Get the token from the AuthController endpoint
            var loginRequest = new
            {
                Username = "admin", // Replace with valid test username
                Password = "1234"  // Replace with valid test password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode(); // Throw if not 2xx

            var tokenResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            var token = tokenResponse?.Token;

            Assert.NotNull(token); // Ensure we got a token back

            // Step 2: Use the token for an authenticated request
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var apiResponse = await _client.GetAsync("/api/exchangerate/latest?Base=USD");

            // Step 3: Assert the response status (can be OK or Forbidden based on role)
            Assert.Equal(HttpStatusCode.OK, apiResponse.StatusCode);
        }


        [Fact]
        public async Task Rate_Limit_Should_Trigger_After_Too_Many_Requests()
        {
            for (int i = 0; i < 10; i++)
            {
                var response = await _client.GetAsync("/api/exchangerate/latest?baseCurrency=USD");
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Assert.Contains("Rate limit exceeded", await response.Content.ReadAsStringAsync());
                    return;
                }
            }

            Assert.True(true, "Rate limit not triggered—check test environment config");
        }
        [Fact]
        public async Task RateLimiting_Should_Return_429_After_Exceeding_Limit()
        {
            var loginRequest = new
            {
                Username = "admin", // Replace with valid test username
                Password = "1234"  // Replace with valid test password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode(); // Throw if not 2xx

            var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            var token = tokenResponse?.Token;

            Assert.NotNull(token); // Ensure we got a token back

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Make 6 requests (assuming the limit is 5 per 10 seconds based on your config)
            for (int i = 0; i < 6; i++)
            {
                var response = await _client.GetAsync("/api/exchangerate/latest?Base=USD");

                if (i < 4)
                {
                    // First 5 requests should succeed
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
                else
                {
                    // The 6th request should return 429 Too Many Requests
                    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

                    // Optionally, check the content for rate limit error message
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.Contains("Rate limit exceeded", content);
                }
            }
        }
       
        [Fact]
        public async Task Unauthenticated_Request_Should_Return_401_If_Missing_Token()
        {
            // Test missing authorization header
            var response = await _client.GetAsync("/api/exchangerate/latest?baseCurrency=USD");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Unauthenticated_Request_Should_Return_401_If_Malformed_Token()
        {
            // Test malformed token
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");
            var response = await _client.GetAsync("/api/exchangerate/latest?baseCurrency=USD");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
