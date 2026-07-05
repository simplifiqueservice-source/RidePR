using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Entities;
using RidePR.Application.Interfaces;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly ApplicationDbContext _context;

    public TripRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Trip trip)
    {
        await _context.Trips.AddAsync(trip);
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _context.Trips.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task UpdateAsync(Trip trip)
    {
        _context.Trips.Update(trip);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
