using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class PassengerRepository : IPassengerRepository
{
    private readonly ApplicationDbContext _context;

    public PassengerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Passenger>> GetPagedAsync(
        string? search,
        bool? active,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(
            _context.Passengers.Include(x => x.User).ThenInclude(x => x.Branch).Include(x => x.Branch),
            search,
            active);

        return await query
            .OrderBy(x => x.User.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        bool? active)
    {
        var query = ApplyFilters(_context.Passengers.AsQueryable(), search, active);

        return await query.CountAsync();
    }

    public async Task<Passenger?> GetByIdAsync(Guid id)
    {
        return await _context.Passengers
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Passenger?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Passengers
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<List<PassengerHistory>> GetHistoryAsync(Guid passengerId)
    {
        return await _context.Set<PassengerHistory>()
            .Where(x => x.PassengerId == passengerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> CpfExistsAsync(string cpf, Guid? ignorePassengerId = null)
    {
        var normalizedCpf = cpf.Trim();
        var query = _context.Passengers.Where(x => x.Cpf == normalizedCpf);

        if (ignorePassengerId.HasValue)
            query = query.Where(x => x.Id != ignorePassengerId.Value);

        return await query.AnyAsync();
    }

    public async Task AddAsync(Passenger passenger)
    {
        await _context.Passengers.AddAsync(passenger);
    }

    public async Task AddHistoryAsync(PassengerHistory history)
    {
        await _context.Set<PassengerHistory>().AddAsync(history);
    }

    public Task UpdateAsync(Passenger passenger)
    {
        _context.Passengers.Update(passenger);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Passenger> ApplyFilters(
        IQueryable<Passenger> query,
        string? search,
        bool? active)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();

            query = query.Where(x =>
                x.Cpf.ToLower().Contains(term) ||
                x.Phone.ToLower().Contains(term) ||
                x.User.Name.ToLower().Contains(term) ||
                x.User.Email.ToLower().Contains(term));
        }

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        return query;
    }
}
