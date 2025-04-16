namespace CurrencyConverter.DTOs
{
    public class GetLatestRateRequestDto
    {
        public required string Base { get; set; }
        public string? Provider { get; set; }
    }
}
