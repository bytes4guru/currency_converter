namespace CurrencyConverter.DTOs
{
    public class HistoricalRatesRequestDto
    {
        public required string BaseCurrency { get; set; }
        public required DateTime Start { get; set; }
        public required DateTime End { get; set; }
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public string? Provider { get; set; }
    }
}
