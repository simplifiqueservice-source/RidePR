using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyTripRequestedAsync(Trip trip);

    Task NotifyTripAcceptedAsync(Trip trip);

    Task NotifyTripStartedAsync(Trip trip);

    Task NotifyTripFinishedAsync(Trip trip);

    Task NotifyDriverLocationUpdatedAsync(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading);
}
