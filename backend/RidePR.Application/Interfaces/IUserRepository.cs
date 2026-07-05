using RidePR.Domain.Entities;
using RidePR.Domain.Enums;

namespace RidePR.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(Guid id);

    Task<List<User>> GetPagedAsync(
        string? search,
        UserRole? role,
        bool? active,
        int page,
        int pageSize);

    Task<int> CountAsync(
        string? search,
        UserRole? role,
        bool? active);

    Task<bool> EmailExistsAsync(string email, Guid? ignoreUserId = null);

    Task AddAsync(User user);

    Task UpdateAsync(User user);

    Task AddRefreshTokenAsync(RefreshToken token);

    Task<RefreshToken?> GetRefreshTokenAsync(string token);

    Task SaveChangesAsync();
}
