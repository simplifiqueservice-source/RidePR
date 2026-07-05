using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task SaveChangesAsync();
}
