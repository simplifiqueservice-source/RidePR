using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetPagedAsync(
        string? search,
        Guid? driverId,
        bool? active,
        int page,
        int pageSize);

    Task<int> CountAsync(
        string? search,
        Guid? driverId,
        bool? active);

    Task<Vehicle?> GetByIdAsync(Guid id);

    Task<bool> PlateExistsAsync(string plate, Guid? ignoreVehicleId = null);

    Task AddAsync(Vehicle vehicle);

    Task UpdateAsync(Vehicle vehicle);

    Task SaveChangesAsync();
}
