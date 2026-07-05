using Microsoft.AspNetCore.SignalR;
using RidePR.Api.Hubs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;

namespace RidePR.Api.Services;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<DriverHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<DriverHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyTripRequestedAsync(Trip trip)
    {
        return SendTripAsync("TripRequested", trip);
    }

    public Task NotifyTripAcceptedAsync(Trip trip)
    {
        return SendTripAsync("TripAccepted", trip);
    }

    public Task NotifyTripStartedAsync(Trip trip)
    {
        return SendTripAsync("TripStarted", trip);
    }

    public Task NotifyTripFinishedAsync(Trip trip)
    {
        return SendTripAsync("TripFinished", trip);
    }

    public Task NotifyDriverLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        return _hub.Clients.All.SendAsync("DriverLocationUpdated", new
        {
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private Task SendTripAsync(string eventName, Trip trip)
    {
        return _hub.Clients.All.SendAsync(eventName, new
        {
            trip.Id,
            trip.PassengerId,
            trip.DriverId,
            Status = trip.Status.ToString(),
            trip.Origin,
            trip.Destination,
            trip.OriginLatitude,
            trip.OriginLongitude,
            trip.DestinationLatitude,
            trip.DestinationLongitude,
            trip.EstimatedDistanceKm,
            trip.EstimatedDurationMinutes,
            trip.ActualDistanceKm,
            trip.Price,
            trip.CreatedAt
        });
    }
}
