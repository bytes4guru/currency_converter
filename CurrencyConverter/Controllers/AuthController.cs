using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;
using CurrencyConverter.ViewModels;

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
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            var Token = _authService.Authenticate(request.Username, request.Password);
            return Ok(new LoginResponse(){ Token=Token });
        }
        catch (ArgumentException ex)
        {
            return BadRequest("Invalid credentials" + ex.Message);
        }
    }
}