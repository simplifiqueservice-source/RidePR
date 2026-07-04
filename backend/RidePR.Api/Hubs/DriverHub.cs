using Microsoft.AspNetCore.SignalR;
using RidePR.Application.Services;

namespace RidePR.Api.Hubs;

public class DriverHub : Hub
{
    private readonly DriverLocationService _locationService;

    public DriverHub(DriverLocationService locationService)
    {
        _locationService = locationService;
    }

    public async Task UpdateLocation(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        await _locationService.UpdateLocationAsync(
            driverId,
            latitude,
            longitude,
            speed,
            heading);

        await Clients.All.SendAsync(
            "DriverLocationUpdated",
            driverId,
            latitude,
            longitude,
            speed,
            heading);
    }
}