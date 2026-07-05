using RidePR.Domain.Entities;
using RidePR.Domain.Enums;

namespace RidePR.Application.Interfaces;

public interface IPaymentRepository
{
    Task<List<Payment>> GetPagedAsync(
        Guid? tripId,
        Guid? passengerId,
        Guid? driverId,
        PaymentMethod? method,
        PaymentStatus? status,
        int page,
        int pageSize);

    Task<int> CountAsync(
        Guid? tripId,
        Guid? passengerId,
        Guid? driverId,
        PaymentMethod? method,
        PaymentStatus? status);

    Task<Payment?> GetByIdAsync(Guid id);

    Task<Payment?> GetByTripIdAsync(Guid tripId);

    Task AddAsync(Payment payment);

    Task UpdateAsync(Payment payment);

    Task SaveChangesAsync();
}
