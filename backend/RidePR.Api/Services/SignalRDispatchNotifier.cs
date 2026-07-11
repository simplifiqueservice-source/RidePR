using Microsoft.AspNetCore.SignalR;
using RidePR.Api.Hubs;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Api.Services;

public class SignalRDispatchNotifier : IDispatchNotifier
{
    private readonly IHubContext<DriverHub> _hub;
    private readonly ILogger<SignalRDispatchNotifier> _logger;

    public SignalRDispatchNotifier(
        IHubContext<DriverHub> hub,
        ILogger<SignalRDispatchNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotifyOfferAsync(Guid driverId, DispatchOfferDto offer)
    {
        _logger.LogInformation(
            "SIGNALR_EVENT_SENT event=DispatchOfferReceived driverId={DriverId} tripId={TripId} group={Group}",
            driverId,
            offer.TripId,
            GetDriverGroup(driverId));

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
