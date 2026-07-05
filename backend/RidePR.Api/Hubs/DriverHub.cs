using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RidePR.Api.Services;
using RidePR.Application.Interfaces;
using RidePR.Application.Services;

namespace RidePR.Api.Hubs;

[Authorize(Roles = "Administrator,Driver,Passenger")]
public class DriverHub : Hub
{
    private readonly DriverLocationService _locationService;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public DriverHub(
        DriverLocationService locationService,
        IRealtimeNotifier realtimeNotifier)
    {
        _locationService = locationService;
        _realtimeNotifier = realtimeNotifier;
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

        await _realtimeNotifier.NotifyDriverLocationUpdatedAsync(
            driverId,
            latitude,
            longitude,
            speed,
            heading);
    }
}
