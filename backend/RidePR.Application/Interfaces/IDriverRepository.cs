using RidePR.Domain.Entities;
using RidePR.Domain.Enums;

namespace RidePR.Application.Interfaces;

public interface IDriverRepository
{
    Task<List<Driver>> GetPagedAsync(
        string? search,
        DriverStatus? status,
        DriverApprovalStatus? approvalStatus,
        bool? active,
        int page,
        int pageSize);

    Task<int> CountAsync(
        string? search,
        DriverStatus? status,
        DriverApprovalStatus? approvalStatus,
        bool? active);

    Task<Driver?> GetByIdAsync(Guid id);

    Task<Driver?> GetByUserIdAsync(Guid userId);

    Task<bool> CpfExistsAsync(string cpf, Guid? ignoreDriverId = null);

    Task<bool> CnhExistsAsync(string cnhNumber, Guid? ignoreDriverId = null);

    Task AddAsync(Driver driver);

    Task UpdateAsync(Driver driver);

    Task DeleteAsync(Driver driver);

    Task SaveChangesAsync();
}
