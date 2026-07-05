using BCrypt.Net;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Shared.Pagination;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserResponseDto>> GetPagedAsync(UserQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var users = await _userRepository.GetPagedAsync(
            query.Search,
            query.Role,
            query.Active,
            page,
            pageSize);

        var total = await _userRepository.CountAsync(
            query.Search,
            query.Role,
            query.Active);

        return new PagedResult<UserResponseDto>
        {
            Items = users.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Result<UserResponseDto>> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return Result<UserResponseDto>.Fail("Usuário não encontrado.");

        return Result<UserResponseDto>.Ok(ToResponse(user));
    }

    public async Task<Result<UserResponseDto>> CreateAsync(RegisterDto dto)
    {
        var emailExists = await _userRepository.EmailExistsAsync(dto.Email);

        if (emailExists)
            return Result<UserResponseDto>.Fail("E-mail já cadastrado.");

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
        await _userRepository.SaveChangesAsync();

        return Result<UserResponseDto>.Ok(ToResponse(user));
    }

    public async Task<Result<UserResponseDto>> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return Result<UserResponseDto>.Fail("Usuário não encontrado.");

        var emailExists = await _userRepository.EmailExistsAsync(dto.Email, id);

        if (emailExists)
            return Result<UserResponseDto>.Fail("E-mail já cadastrado.");

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Role = dto.Role;
        user.Active = dto.Active;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<UserResponseDto>.Ok(ToResponse(user));
    }

    public async Task<Result> ChangePasswordAsync(Guid id, ChangeUserPasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return Result.Fail("Usuário não encontrado.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result.Ok("Senha alterada com sucesso.");
    }

    public async Task<Result> DeactivateAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
            return Result.Fail("Usuário não encontrado.");

        user.Active = false;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result.Ok("Usuário desativado com sucesso.");
    }

    private static UserResponseDto ToResponse(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Active = user.Active,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }
}
