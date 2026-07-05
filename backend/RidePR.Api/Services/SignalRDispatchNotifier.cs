using Microsoft.AspNetCore.SignalR;
using RidePR.Api.Hubs;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Api.Services;

public class SignalRDispatchNotifier : IDispatchNotifier
{
    private readonly IHubContext<DriverHub> _hub;

    public SignalRDispatchNotifier(IHubContext<DriverHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyOfferAsync(Guid driverId, DispatchOfferDto offer)
    {
        await _hub.Clients.Group(GetDriverGroup(driverId)).SendAsync("DispatchOfferReceived", offer);
    }

    public async Task NotifyOfferExpiredAsync(Guid driverId, Guid tripId)
    {
        await _hub.Clients.Group(GetDriverGroup(driverId)).SendAsync("DispatchOfferExpired", tripId);
    }

    public async Task NotifyDispatchUpdatedAsync(DispatchStateDto state)
    {
        await _hub.Clients.All.SendAsync("DispatchUpdated", state);
    }

    public static string GetDriverGroup(Guid driverId)
    {
        return $"driver:{driverId}";
    }
}
