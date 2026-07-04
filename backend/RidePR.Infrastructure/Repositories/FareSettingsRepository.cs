using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class FareSettingsRepository : IFareSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public FareSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FareSettings?> GetActiveAsync()
    {
        return await _context.FareSettings
            .FirstOrDefaultAsync(x => x.Active);
    }

    public async Task<List<FareSettings>> GetAllAsync()
    {
        return await _context.FareSettings
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task AddAsync(FareSettings settings)
    {
        await _context.FareSettings.AddAsync(settings);
    }

    public Task UpdateAsync(FareSettings settings)
    {
        _context.FareSettings.Update(settings);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}