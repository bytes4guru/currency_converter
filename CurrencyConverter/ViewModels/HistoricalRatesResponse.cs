namespace CurrencyConverter.ViewModels
{
    public class HistoricalRatesResponse
    {
        public float Amount { get; set; }
        public string Base { get; set; } = string.Empty;
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();
        public int TotalRecords { get; set; }
    }
}
