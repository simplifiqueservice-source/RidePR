using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetPagedAsync(
        Guid? tripId,
        Guid? passengerId,
        Guid? driverId,
        PaymentMethod? method,
        PaymentStatus? status,
        int page,
        int pageSize)
    {
        var query = ApplyFilters(_context.Payments.AsQueryable(), tripId, passengerId, driverId, method, status);

        return await query
            .Include(x => x.Splits)
            .Include(x => x.Refunds)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        Guid? tripId,
        Guid? passengerId,
        Guid? driverId,
        PaymentMethod? method,
        PaymentStatus? status)
    {
        var query = ApplyFilters(_context.Payments.AsQueryable(), tripId, passengerId, driverId, method, status);

        return await query.CountAsync();
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(x => x.Splits)
            .Include(x => x.Refunds)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Payment?> GetByTripIdAsync(Guid tripId)
    {
        return await _context.Payments
            .Include(x => x.Splits)
            .Include(x => x.Refunds)
            .FirstOrDefaultAsync(x => x.TripId == tripId);
    }

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private static IQueryable<Payment> ApplyFilters(
        IQueryable<Payment> query,
        Guid? tripId,
        Guid? passengerId,
        Guid? driverId,
        PaymentMethod? method,
        PaymentStatus? status)
    {
        if (tripId.HasValue)
            query = query.Where(x => x.TripId == tripId.Value);

        if (passengerId.HasValue)
            query = query.Where(x => x.PassengerId == passengerId.Value);

        if (driverId.HasValue)
            query = query.Where(x => x.DriverId == driverId.Value);

        if (method.HasValue)
            query = query.Where(x => x.Method == method.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return query;
    }
}
