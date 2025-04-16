namespace CurrencyConverter.DTOs
{
    public class HistoricalRatesResponseDto
    {
        public required decimal Amount { get; set; }
        public required string Base { get; set; }
        public required DateTime Start_Date { get; set; }
        public required DateTime End_Date { get; set; }
        public required Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
        public int TotalRecords { get; set; }
    }
}
