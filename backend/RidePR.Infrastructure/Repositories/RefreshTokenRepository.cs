using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.Set<RefreshToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _context.Set<RefreshToken>().AddAsync(token);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
