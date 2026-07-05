using BCrypt.Net;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<Result<LoginResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);

        if (existingUser != null)
            return Result<LoginResponseDto>.Fail("E-mail já cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user);

        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _userRepository.SaveChangesAsync();

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null || !user.Active)
            return Result<LoginResponseDto>.Fail("E-mail ou senha inválidos.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Result<LoginResponseDto>.Fail("E-mail ou senha inválidos.");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user);

        await _userRepository.AddRefreshTokenAsync(refreshToken);
        await _userRepository.SaveChangesAsync();

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
    }

    public async Task<Result<LoginResponseDto>> RefreshAsync(RefreshTokenDto dto)
    {
        var refreshToken = await _userRepository.GetRefreshTokenAsync(dto.RefreshToken);

        if (refreshToken == null || refreshToken.Revoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            return Result<LoginResponseDto>.Fail("Refresh token inválido.");

        var user = refreshToken.User;

        refreshToken.Revoked = true;

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken(user);

        await _userRepository.AddRefreshTokenAsync(newRefreshToken);
        await _userRepository.SaveChangesAsync();

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        });
    }

    public async Task<Result> LogoutAsync(RefreshTokenDto dto)
    {
        var refreshToken = await _userRepository.GetRefreshTokenAsync(dto.RefreshToken);

        if (refreshToken == null)
            return Result.Fail("Refresh token inválido.");

        refreshToken.Revoked = true;

        await _userRepository.SaveChangesAsync();

        return Result.Ok("Logout realizado com sucesso.");
    }
}