using Microsoft.EntityFrameworkCore;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class AdminPanelRepository : IAdminPanelRepository
{
    private readonly ApplicationDbContext _context;

    public AdminPanelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUsersOverviewDto> GetUsersOverviewAsync(DateTime from, DateTime to)
    {
        return new AdminUsersOverviewDto
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveUsers = await _context.Users.CountAsync(x => x.Active),
            NewUsers = await _context.Users.CountAsync(x => x.CreatedAt >= from && x.CreatedAt <= to),
            Administrators = await _context.Users.CountAsync(x => x.Role == UserRole.Administrator)
        };
    }

    public async Task<AdminDriversOverviewDto> GetDriversOverviewAsync()
    {
        return new AdminDriversOverviewDto
        {
            TotalDrivers = await _context.Drivers.CountAsync(),
            ActiveDrivers = await _context.Drivers.CountAsync(x => x.Active),
            PendingApproval = await _context.Drivers.CountAsync(x => x.ApprovalStatus == DriverApprovalStatus.Pending),
            ApprovedDrivers = await _context.Drivers.CountAsync(x => x.ApprovalStatus == DriverApprovalStatus.Approved),
            OnlineDrivers = await _context.Drivers.CountAsync(x => x.Status == DriverStatus.Online),
            BusyDrivers = await _context.Drivers.CountAsync(x => x.Status == DriverStatus.Busy)
        };
    }

    public async Task<AdminTripsOverviewDto> GetTripsOverviewAsync(DateTime from, DateTime to)
    {
        var total = await _context.Trips.CountAsync(x => x.CreatedAt >= from && x.CreatedAt <= to);
        var finished = await _context.Trips.CountAsync(x =>
            x.CreatedAt >= from &&
            x.CreatedAt <= to &&
            x.Status == TripStatus.Finished);

        return new AdminTripsOverviewDto
        {
            TotalTrips = total,
            RequestedTrips = await CountTripsAsync(from, to, TripStatus.Requested),
            AcceptedTrips = await CountTripsAsync(from, to, TripStatus.Accepted),
            InProgressTrips = await CountTripsAsync(from, to, TripStatus.InProgress),
            FinishedTrips = finished,
            CancelledTrips = await CountTripsAsync(from, to, TripStatus.Cancelled),
            CompletionRate = total == 0 ? 0 : Math.Round(finished * 100m / total, 2)
        };
    }

    public async Task<AdminPaymentsOverviewDto> GetPaymentsOverviewAsync(DateTime from, DateTime to)
    {
        var payments = _context.Payments.Where(x => x.CreatedAt >= from && x.CreatedAt <= to);
        var paidPayments = payments.Where(x =>
            x.Status == PaymentStatus.Paid ||
            x.Status == PaymentStatus.PartiallyRefunded ||
            x.Status == PaymentStatus.Refunded);

        var grossRevenue = await paidPayments.SumAsync(x => x.Amount);
        var refundedAmount = await payments.SumAsync(x => x.RefundedAmount);

        return new AdminPaymentsOverviewDto
        {
            TotalPayments = await payments.CountAsync(),
            PaidPayments = await payments.CountAsync(x => x.Status == PaymentStatus.Paid),
            PendingPayments = await payments.CountAsync(x => x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Authorized),
            FailedPayments = await payments.CountAsync(x => x.Status == PaymentStatus.Failed || x.Status == PaymentStatus.Cancelled),
            RefundedPayments = await payments.CountAsync(x =>
                x.Status == PaymentStatus.Refunded ||
                x.Status == PaymentStatus.PartiallyRefunded),
            GrossRevenue = grossRevenue,
            RefundedAmount = refundedAmount,
            NetRevenue = grossRevenue - refundedAmount
        };
    }

    public async Task<AdminOperationsDto> GetOperationsAsync()
    {
        return new AdminOperationsDto
        {
            PendingDriverApprovals = await _context.Drivers.CountAsync(x => x.ApprovalStatus == DriverApprovalStatus.Pending),
            ActiveTrips = await _context.Trips.CountAsync(x =>
                x.Status == TripStatus.Requested ||
                x.Status == TripStatus.Accepted ||
                x.Status == TripStatus.InProgress),
            PendingPayments = await _context.Payments.CountAsync(x =>
                x.Status == PaymentStatus.Pending ||
                x.Status == PaymentStatus.Authorized),
            PendingRefunds = await _context.PaymentRefunds.CountAsync(x => x.Status == RefundStatus.Pending),
            OnlineDrivers = await _context.DriverLocations.CountAsync(x => x.Online),
            AvailableDrivers = await _context.Drivers.CountAsync(x =>
                x.Active &&
                x.ApprovalStatus == DriverApprovalStatus.Approved &&
                x.Status == DriverStatus.Online)
        };
    }

    public async Task<IReadOnlyList<AdminRevenuePointDto>> GetRevenueAsync(DateTime from, DateTime to)
    {
        var rows = await _context.Payments
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .Where(x =>
                x.Status == PaymentStatus.Paid ||
                x.Status == PaymentStatus.PartiallyRefunded ||
                x.Status == PaymentStatus.Refunded)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(x => new
            {
                Date = x.Key,
                GrossRevenue = x.Sum(p => p.Amount),
                RefundedAmount = x.Sum(p => p.RefundedAmount),
                PaidPayments = x.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return rows
            .Select(x => new AdminRevenuePointDto
            {
                Date = x.Date,
                GrossRevenue = x.GrossRevenue,
                RefundedAmount = x.RefundedAmount,
                NetRevenue = x.GrossRevenue - x.RefundedAmount,
                PaidPayments = x.PaidPayments
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AdminRecentActivityDto>> GetRecentActivityAsync(int limit)
    {
        var users = await _context.Users
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new AdminRecentActivityDto
            {
                Id = x.Id,
                Type = "User",
                Title = x.Name,
                Description = x.Email,
                Status = x.Active ? "Active" : "Inactive",
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var tripRows = await _context.Trips
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new
            {
                Id = x.Id,
                x.Origin,
                x.Destination,
                x.Status,
                x.Price,
                x.CreatedAt
            })
            .ToListAsync();

        var trips = tripRows
            .Select(x => new AdminRecentActivityDto
            {
                Id = x.Id,
                Type = "Trip",
                Title = x.Origin,
                Description = x.Destination,
                Status = x.Status.ToString(),
                Amount = x.Price,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var paymentRows = await _context.Payments
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new
            {
                Id = x.Id,
                x.Method,
                x.ProviderPaymentId,
                x.Provider,
                x.Status,
                x.Amount,
                x.CreatedAt
            })
            .ToListAsync();

        var payments = paymentRows
            .Select(x => new AdminRecentActivityDto
            {
                Id = x.Id,
                Type = "Payment",
                Title = x.Method.ToString(),
                Description = x.ProviderPaymentId ?? x.Provider ?? "Payment",
                Status = x.Status.ToString(),
                Amount = x.Amount,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return users
            .Concat(trips)
            .Concat(payments)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<AdminLiveDriverDto>> GetLiveDriversAsync(bool onlineOnly, int limit)
    {
        var query =
            from driver in _context.Drivers
            join user in _context.Users on driver.UserId equals user.Id
            join location in _context.DriverLocations on driver.Id equals location.DriverId
            where !onlineOnly || location.Online
            orderby location.UpdatedAt descending
            select new
            {
                driver.Id,
                driver.UserId,
                user.Name,
                driver.Phone,
                driver.Status,
                driver.ApprovalStatus,
                location.Online,
                location.Position,
                location.Speed,
                location.Heading,
                location.UpdatedAt
            };

        var rows = await query.Take(limit).ToListAsync();

        return rows
            .Select(x => new AdminLiveDriverDto
            {
                DriverId = x.Id,
                UserId = x.UserId,
                Name = x.Name,
                Phone = x.Phone,
                Status = x.Status.ToString(),
                ApprovalStatus = x.ApprovalStatus.ToString(),
                Online = x.Online,
                Latitude = x.Position.Y,
                Longitude = x.Position.X,
                Speed = x.Speed,
                Heading = x.Heading,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();
    }

    private async Task<int> CountTripsAsync(DateTime from, DateTime to, TripStatus status)
    {
        return await _context.Trips.CountAsync(x =>
            x.CreatedAt >= from &&
            x.CreatedAt <= to &&
            x.Status == status);
    }
}
