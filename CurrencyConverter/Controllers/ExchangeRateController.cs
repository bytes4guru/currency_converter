using CurrencyConverter.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.DTOs;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRateController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }
    
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates([FromQuery] GetLatestRateRequestDto dto)
    {
        try
        {
            var result = await _exchangeRateService.GetLatestRatesAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) {
            return BadRequest(ex.Message);
        }
        
    }
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("convert")]
    public async Task<IActionResult> ConvertCurrency([FromQuery] ConvertCurrencyRequestDto dto)
    {
        try
        {
            var result = await _exchangeRateService.ConvertCurrencyAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch(Exception ex)
        {
            return Problem(ex.Message);
        }
    }
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistoricalRates([FromQuery] HistoricalRatesRequestDto requestDto)
    {
        try
        {
            var result = await _exchangeRateService.GetHistoricalRatesAsync(requestDto);
            return Ok(result);
        }
        catch (ExchangeRateApiException ex)
        {
            return Problem(ex.Message);
        }
        catch (ArgumentException ex) {
            return BadRequest(ex.Message);
        }
    }
}