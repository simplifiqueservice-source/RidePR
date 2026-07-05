using RidePR.Application.DTOs;

namespace RidePR.Application.Interfaces;

public interface IDispatchNotifier
{
    Task NotifyOfferAsync(Guid driverId, DispatchOfferDto offer);

    Task NotifyOfferExpiredAsync(Guid driverId, Guid tripId);

    Task NotifyDispatchUpdatedAsync(DispatchStateDto state);
}
