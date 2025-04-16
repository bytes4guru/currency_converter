namespace CurrencyConverter.DTOs
{
    public class ConvertCurrencyRequestDto
    {
        public required string From {  get; set; }
        public required string To { get; set; }
        public required decimal Amount { get; set; }

        public string? Provider { get; set; }
    }
}
