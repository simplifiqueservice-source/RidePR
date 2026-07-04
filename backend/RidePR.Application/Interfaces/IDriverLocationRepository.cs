using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IDriverLocationRepository
{
    Task<DriverLocation?> GetByDriverIdAsync(Guid driverId);

    Task<List<DriverLocation>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm);

    Task AddAsync(DriverLocation location);

    Task UpdateAsync(DriverLocation location);

    Task SaveChangesAsync();
}