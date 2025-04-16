using CurrencyConverter.DTOs;
public interface IExchangeRateService
{
    Task<LatestExchangeRateResponseDto> GetLatestRatesAsync(GetLatestRateRequestDto dto);
    Task<ConvertCurrencyResponseDto> ConvertCurrencyAsync(ConvertCurrencyRequestDto dto);
    Task<HistoricalRatesResponseDto> GetHistoricalRatesAsync(HistoricalRatesRequestDto dto);
}


