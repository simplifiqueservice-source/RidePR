using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);

    Task<User?> GetByIdAsync(Guid id);

    Task AddAsync(User user);

    Task UpdateAsync(User user);

    Task AddRefreshTokenAsync(RefreshToken token);

    Task<RefreshToken?> GetRefreshTokenAsync(string token);

    Task SaveChangesAsync();
}