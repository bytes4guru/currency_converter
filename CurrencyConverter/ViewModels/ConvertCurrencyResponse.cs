namespace CurrencyConverter.ViewModels
{
    public class ConvertCurrencyResponse
    {
        public float Amount { get; set; }
        public string Base { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

}
