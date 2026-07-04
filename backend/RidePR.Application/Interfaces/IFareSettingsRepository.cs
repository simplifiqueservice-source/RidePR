using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IFareSettingsRepository
{
    Task<FareSettings?> GetActiveAsync();

    Task<List<FareSettings>> GetAllAsync();

    Task AddAsync(FareSettings settings);

    Task UpdateAsync(FareSettings settings);

    Task SaveChangesAsync();
}