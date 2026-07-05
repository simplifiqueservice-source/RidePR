using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vehicle>> GetPagedAsync(
        string? search,
        Guid? driverId,
        bool? active,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(_context.Vehicles.Include(x => x.Driver).ThenInclude(x => x.User), search, driverId, active);

        return await query
            .OrderBy(x => x.Plate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        Guid? driverId,
        bool? active)
    {
        var query = ApplyFilters(_context.Vehicles.AsQueryable(), search, driverId, active);

        return await query.CountAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _context.Vehicles
            .Include(x => x.Driver)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> PlateExistsAsync(string plate, Guid? ignoreVehicleId = null)
    {
        var normalizedPlate = plate.Trim().ToUpperInvariant();
        var query = _context.Vehicles.Where(x => x.Plate == normalizedPlate);

        if (ignoreVehicleId.HasValue)
            query = query.Where(x => x.Id != ignoreVehicleId.Value);

        return await query.AnyAsync();
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
    }

    public Task UpdateAsync(Vehicle vehicle)
    {
        _context.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Vehicle> ApplyFilters(
        IQueryable<Vehicle> query,
        string? search,
        Guid? driverId,
        bool? active)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();

            query = query.Where(x =>
                x.Plate.ToLower().Contains(term) ||
                x.Model.ToLower().Contains(term) ||
                x.Brand.ToLower().Contains(term) ||
                x.Renavam.ToLower().Contains(term) ||
                x.Chassis.ToLower().Contains(term) ||
                x.Driver.User.Name.ToLower().Contains(term));
        }

        if (driverId.HasValue)
            query = query.Where(x => x.DriverId == driverId.Value);

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        return query;
    }
}
