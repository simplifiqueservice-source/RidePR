using RidePR.Application.DTOs;

namespace RidePR.Application.Interfaces;

public interface IDispatchQueue
{
    Task<DispatchStateDto?> GetAsync(Guid tripId);

    Task SetAsync(DispatchStateDto state);

    Task RemoveAsync(Guid tripId);

    Task<List<Guid>> GetActiveTripIdsAsync();
}
