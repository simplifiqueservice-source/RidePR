using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);

    RefreshToken GenerateRefreshToken(User user);
}