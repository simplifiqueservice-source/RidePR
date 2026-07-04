using RidePR.Domain.Entities;

namespace RidePR.Application.Services;

public class DispatchService
{
    private readonly DriverLocationService _locationService;

    public DispatchService(DriverLocationService locationService)
    {
        _locationService = locationService;
    }

    // =====================================================
    // Procura motoristas próximos
    // =====================================================
    public async Task<List<DriverLocation>> FindNearbyDriversAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var drivers = await _locationService.GetNearbyDriversAsync(
            latitude,
            longitude,
            radiusKm);

        return drivers
            .OrderBy(x => x.UpdatedAt)
            .ToList();
    }
}