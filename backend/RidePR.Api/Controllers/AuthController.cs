using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
            return Unauthorized(result.Message);

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto)
    {
        var result = await _authService.RefreshAsync(dto);

        if (!result.Success)
            return Unauthorized(result.Message);

        return Ok(result.Data);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenDto dto)
    {
        var result = await _authService.LogoutAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Message);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated,
            name = User.Identity?.Name,
            claims = User.Claims.Select(x => new
            {
                x.Type,
                x.Value
            })
        });
    }
}