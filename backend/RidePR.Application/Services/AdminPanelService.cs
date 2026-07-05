using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class AdminPanelService
{
    private readonly IAdminPanelRepository _adminPanelRepository;

    public AdminPanelService(IAdminPanelRepository adminPanelRepository)
    {
        _adminPanelRepository = adminPanelRepository;
    }

    public async Task<Result<AdminDashboardDto>> GetDashboardAsync(AdminDashboardQueryDto query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);

        var users = await _adminPanelRepository.GetUsersOverviewAsync(from, to);
        var drivers = await _adminPanelRepository.GetDriversOverviewAsync();
        var trips = await _adminPanelRepository.GetTripsOverviewAsync(from, to);
        var payments = await _adminPanelRepository.GetPaymentsOverviewAsync(from, to);
        var operations = await _adminPanelRepository.GetOperationsAsync();
        var revenue = await _adminPanelRepository.GetRevenueAsync(from, to);
        var recentActivity = await _adminPanelRepository.GetRecentActivityAsync(10);

        return Result<AdminDashboardDto>.Ok(new AdminDashboardDto
        {
            GeneratedAt = DateTime.UtcNow,
            From = from,
            To = to,
            Users = users,
            Drivers = drivers,
            Trips = trips,
            Payments = payments,
            Operations = operations,
            Revenue = revenue,
            RecentActivity = recentActivity
        });
    }

    public async Task<Result<AdminOperationsDto>> GetOperationsAsync()
    {
        var operations = await _adminPanelRepository.GetOperationsAsync();

        return Result<AdminOperationsDto>.Ok(operations);
    }

    public async Task<Result<IReadOnlyList<AdminRevenuePointDto>>> GetRevenueAsync(AdminDashboardQueryDto query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);
        var revenue = await _adminPanelRepository.GetRevenueAsync(from, to);

        return Result<IReadOnlyList<AdminRevenuePointDto>>.Ok(revenue);
    }

    public async Task<Result<IReadOnlyList<AdminRecentActivityDto>>> GetRecentActivityAsync(AdminRecentActivityQueryDto query)
    {
        var activity = await _adminPanelRepository.GetRecentActivityAsync(NormalizeLimit(query.Limit, 1, 100));

        return Result<IReadOnlyList<AdminRecentActivityDto>>.Ok(activity);
    }

    public async Task<Result<IReadOnlyList<AdminLiveDriverDto>>> GetLiveDriversAsync(AdminLiveDriversQueryDto query)
    {
        var drivers = await _adminPanelRepository.GetLiveDriversAsync(
            query.OnlineOnly,
            NormalizeLimit(query.Limit, 1, 500));

        return Result<IReadOnlyList<AdminLiveDriverDto>>.Ok(drivers);
    }

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var normalizedTo = (to ?? DateTime.UtcNow).ToUniversalTime();
        var normalizedFrom = (from ?? normalizedTo.AddDays(-30)).ToUniversalTime();

        if (normalizedFrom > normalizedTo)
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);

        return (normalizedFrom, normalizedTo);
    }

    private static int NormalizeLimit(int limit, int min, int max)
    {
        if (limit < min)
            return min;

        return limit > max ? max : limit;
    }
}
