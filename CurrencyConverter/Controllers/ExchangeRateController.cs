using CurrencyConverter.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.ViewModels;

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
    public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency)
    {
        try
        {
            var result = await _exchangeRateService.GetLatestRatesAsync(baseCurrency);
            return Ok(result);
        }
        catch (Exception ex) {
            return BadRequest(ex.Message);
        }
        
    }
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("convert")]
    public async Task<IActionResult> ConvertCurrency([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount)
    {
        try
        {
            var result = await _exchangeRateService.ConvertCurrencyAsync(from, to, amount);
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
    public async Task<IActionResult> GetHistoricalRates([FromQuery] string baseCurrency, [FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _exchangeRateService.GetHistoricalRatesAsync(baseCurrency, start, end, page, pageSize);
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