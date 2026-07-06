using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly ApplicationDbContext _context;

    public DriverRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Driver>> GetPagedAsync(
        string? search,
        DriverStatus? status,
        DriverApprovalStatus? approvalStatus,
        bool? active,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(
            _context.Drivers.Include(x => x.User).ThenInclude(x => x.Branch).Include(x => x.Branch),
            search,
            status,
            approvalStatus,
            active);

        return await query
            .OrderBy(x => x.User.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        DriverStatus? status,
        DriverApprovalStatus? approvalStatus,
        bool? active)
    {
        var query = ApplyFilters(_context.Drivers.AsQueryable(), search, status, approvalStatus, active);

        return await query.CountAsync();
    }

    public async Task<Driver?> GetByIdAsync(Guid id)
    {
        return await _context.Drivers
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Driver?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Drivers
            .Include(x => x.User)
            .ThenInclude(x => x.Branch)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<bool> CpfExistsAsync(string cpf, Guid? ignoreDriverId = null)
    {
        var normalizedCpf = cpf.Trim();
        var query = _context.Drivers.Where(x => x.Cpf == normalizedCpf);

        if (ignoreDriverId.HasValue)
            query = query.Where(x => x.Id != ignoreDriverId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> CnhExistsAsync(string cnhNumber, Guid? ignoreDriverId = null)
    {
        var normalizedCnh = cnhNumber.Trim();
        var query = _context.Drivers.Where(x => x.CnhNumber == normalizedCnh);

        if (ignoreDriverId.HasValue)
            query = query.Where(x => x.Id != ignoreDriverId.Value);

        return await query.AnyAsync();
    }

    public async Task AddAsync(Driver driver)
    {
        await _context.Drivers.AddAsync(driver);
    }

    public Task UpdateAsync(Driver driver)
    {
        _context.Drivers.Update(driver);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Driver driver)
    {
        _context.Drivers.Remove(driver);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Driver> ApplyFilters(
        IQueryable<Driver> query,
        string? search,
        DriverStatus? status,
        DriverApprovalStatus? approvalStatus,
        bool? active)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();

            query = query.Where(x =>
                x.Cpf.ToLower().Contains(term) ||
                x.CnhNumber.ToLower().Contains(term) ||
                x.Phone.ToLower().Contains(term) ||
                x.User.Name.ToLower().Contains(term) ||
                x.User.Email.ToLower().Contains(term));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (approvalStatus.HasValue)
            query = query.Where(x => x.ApprovalStatus == approvalStatus.Value);

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        return query;
    }
}
