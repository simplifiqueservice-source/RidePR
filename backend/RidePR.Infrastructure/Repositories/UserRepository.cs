using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<User>> GetPagedAsync(
        string? search,
        UserRole? role,
        bool? active,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(_context.Users.AsQueryable(), search, role, active);

        return await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        UserRole? role,
        bool? active)
    {
        var query = ApplyFilters(_context.Users.AsQueryable(), search, role, active);

        return await query.CountAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? ignoreUserId = null)
    {
        var query = _context.Users
            .Where(x => x.Email.ToLower() == email.ToLower());

        if (ignoreUserId.HasValue)
            query = query.Where(x => x.Id != ignoreUserId.Value);

        return await query.AnyAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _context.Set<RefreshToken>().AddAsync(token);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.Set<RefreshToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static IQueryable<User> ApplyFilters(
        IQueryable<User> query,
        string? search,
        UserRole? role,
        bool? active)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();

            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term));
        }

        if (role.HasValue)
            query = query.Where(x => x.Role == role.Value);

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        return query;
    }
}
