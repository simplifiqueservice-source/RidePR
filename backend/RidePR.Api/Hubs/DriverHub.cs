using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RidePR.Api.Services;
using RidePR.Application.Services;

namespace RidePR.Api.Hubs;

[Authorize(Roles = "Administrator,Driver")]
public class DriverHub : Hub
{
    private readonly DriverLocationService _locationService;

    public DriverHub(DriverLocationService locationService)
    {
        _locationService = locationService;
    }

    public async Task JoinDriverGroup(Guid driverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRDispatchNotifier.GetDriverGroup(driverId));
    }

    public async Task LeaveDriverGroup(Guid driverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRDispatchNotifier.GetDriverGroup(driverId));
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
