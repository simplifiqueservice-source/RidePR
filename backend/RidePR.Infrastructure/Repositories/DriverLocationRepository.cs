using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class DriverLocationRepository : IDriverLocationRepository
{
    private readonly ApplicationDbContext _context;

    public DriverLocationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DriverLocation?> GetByDriverIdAsync(Guid driverId)
    {
        return await _context.DriverLocations
            .FirstOrDefaultAsync(x => x.DriverId == driverId);
    }

    public async Task<List<DriverLocation>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var point = new Point(longitude, latitude)
        {
            SRID = 4326
        };

        return await _context.DriverLocations
            .Where(x =>
                x.Online &&
                x.Position.Distance(point) <= (radiusKm / 111.32))
            .OrderBy(x => x.Position.Distance(point))
            .ToListAsync();
    }

    public async Task AddAsync(DriverLocation location)
    {
        await _context.DriverLocations.AddAsync(location);
    }

    public Task UpdateAsync(DriverLocation location)
    {
        _context.DriverLocations.Update(location);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}