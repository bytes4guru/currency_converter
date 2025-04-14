using System.Text.Json;
using CurrencyConverter.Exceptions;

namespace CurrencyConverter.Helpers
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetAndDeserializeAsync<T>(
            this HttpClient client,
            string url,
            ILogger logger,
            string errorContext = "API")
        {
            HttpResponseMessage httpResponse;

            try
            {
                httpResponse = await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HTTP request to {Url} failed", url);
                throw new ExchangeRateApiException($"Failed to reach {errorContext}: {ex.Message}");
            }

            var content = await httpResponse.Content.ReadAsStringAsync();

            // Check if the HTTP request was unsuccessful
            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogError("HTTP request failed with status {StatusCode}: {Content}", httpResponse.StatusCode, content);
                throw new ExchangeRateApiException($"{errorContext} failed with HTTP {httpResponse.StatusCode}");
            }

            // Check if the response content contains an error message
            if (IsErrorMessage(content, out var message))
            {
                logger.LogWarning("API returned error message: {Message}", message);
                throw new ExchangeRateApiException($"{errorContext} error: {message}");
            }

            // Deserialize the content into the expected result type
            try
            {
                var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    logger.LogError("Deserialized object is null. Content: {Content}", content);
                    throw new ExchangeRateApiException($"Unable to deserialize response from {errorContext}");
                }

                return result;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Deserialization failed for content: {Content}", content);
                throw new ExchangeRateApiException($"Failed to parse JSON from {errorContext}");
            }
        }

        private static bool IsErrorMessage(string content, out string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var element))
                {
                    message = element.GetString() ?? "Unknown error";
                    return true;
                }
            }
            catch (JsonException ex)
            {
                // Not a valid JSON error message, log parsing error.
                message = $"Error while parsing error message: {ex.Message}";
            }

            message = string.Empty;
            return false;
        }
    }
}
