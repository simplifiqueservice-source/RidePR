using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface ITripRepository
{
    Task AddAsync(Trip trip);

    Task<Trip?> GetByIdAsync(Guid id);

    Task UpdateAsync(Trip trip);

    Task SaveChangesAsync();
}
