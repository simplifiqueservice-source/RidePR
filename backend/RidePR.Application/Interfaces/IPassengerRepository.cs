using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IPassengerRepository
{
    Task<List<Passenger>> GetPagedAsync(
        string? search,
        bool? active,
        int page,
        int pageSize);

    Task<int> CountAsync(
        string? search,
        bool? active);

    Task<Passenger?> GetByIdAsync(Guid id);

    Task<Passenger?> GetByUserIdAsync(Guid userId);

    Task<List<PassengerHistory>> GetHistoryAsync(Guid passengerId);

    Task<bool> CpfExistsAsync(string cpf, Guid? ignorePassengerId = null);

    Task AddAsync(Passenger passenger);

    Task AddHistoryAsync(PassengerHistory history);

    Task UpdateAsync(Passenger passenger);

    Task SaveChangesAsync();
}
