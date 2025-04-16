using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.DTOs;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto requestDto)
    {
        try
        {
            var Token = _authService.Authenticate(requestDto);
            return Ok(new LoginResponseDto(){ Token=Token });
        }
        catch (ArgumentException ex)
        {
            return BadRequest("Invalid credentials: " + ex.Message);
        }
    }
}