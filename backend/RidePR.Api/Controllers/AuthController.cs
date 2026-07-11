using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;
using RidePR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;

namespace RidePR.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(AuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        await EnsureProfileForRegisteredUserAsync(result.Data!.UserId, dto.Role);

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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user == null)
            return Ok(new { Message = "Se o e-mail existir, a senha sera redefinida." });

        var newPassword = string.IsNullOrWhiteSpace(dto.NewPassword)
            ? $"RidePR{DateTime.UtcNow:HHmmss}!"
            : dto.NewPassword;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Senha redefinida com sucesso.",
            TemporaryPassword = newPassword
        });
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

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    private async Task EnsureProfileForRegisteredUserAsync(Guid userId, UserRole role)
    {
        if (role == UserRole.Passenger)
        {
            if (await _context.Passengers.AnyAsync(x => x.UserId == userId))
                return;

            await _context.Passengers.AddAsync(new Passenger
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Cpf = TemporaryDocument("P"),
                BirthDate = DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-18).Date, DateTimeKind.Utc),
                Active = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return;
        }

        if (role == UserRole.Driver)
        {
            if (await _context.Drivers.AnyAsync(x => x.UserId == userId))
                return;

            var token = TemporaryDocument("D");
            await _context.Drivers.AddAsync(new Driver
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Cpf = token,
                BirthDate = DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-18).Date, DateTimeKind.Utc),
                CnhNumber = token,
                CnhExpiration = DateTime.SpecifyKind(DateTime.UtcNow.AddYears(1).Date, DateTimeKind.Utc),
                Status = DriverStatus.Offline,
                ApprovalStatus = DriverApprovalStatus.Pending,
                Active = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    private static string TemporaryDocument(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..14].ToUpperInvariant();
    }
}
