using NetTopologySuite.Geometries;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;

namespace RidePR.Application.Services;

public class DriverLocationService
{
    private readonly IDriverLocationRepository _repository;

    public DriverLocationService(IDriverLocationRepository repository)
    {
        _repository = repository;
    }

    // ==========================
    // Atualiza localização
    // ==========================
    public async Task UpdateLocationAsync(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        var location = await _repository.GetByDriverIdAsync(driverId);

        if (location == null)
        {
            location = new DriverLocation
            {
                Id = Guid.NewGuid(),
                DriverId = driverId,
                Position = new Point(longitude, latitude)
                {
                    SRID = 4326
                },
                Speed = speed,
                Heading = heading,
                Online = true,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(location);
        }
        else
        {
            location.Position = new Point(longitude, latitude)
            {
                SRID = 4326
            };

            location.Speed = speed;
            location.Heading = heading;
            location.Online = true;
            location.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(location);
        }

        await _repository.SaveChangesAsync();
    }

    // ==========================
    // Busca motoristas próximos
    // ==========================
    public async Task<List<DriverLocation>> GetNearbyDriversAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        return await _repository.GetNearbyAsync(
            latitude,
            longitude,
            radiusKm);
    }

    // ==========================
    // Busca localização
    // ==========================
    public async Task<DriverLocation?> GetDriverLocationAsync(Guid driverId)
    {
        return await _repository.GetByDriverIdAsync(driverId);
    }
}