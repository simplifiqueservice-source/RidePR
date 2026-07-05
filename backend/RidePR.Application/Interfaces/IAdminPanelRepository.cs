using RidePR.Application.DTOs;

namespace RidePR.Application.Interfaces;

public interface IAdminPanelRepository
{
    Task<AdminUsersOverviewDto> GetUsersOverviewAsync(DateTime from, DateTime to);

    Task<AdminDriversOverviewDto> GetDriversOverviewAsync();

    Task<AdminTripsOverviewDto> GetTripsOverviewAsync(DateTime from, DateTime to);

    Task<AdminPaymentsOverviewDto> GetPaymentsOverviewAsync(DateTime from, DateTime to);

    Task<AdminOperationsDto> GetOperationsAsync();

    Task<IReadOnlyList<AdminRevenuePointDto>> GetRevenueAsync(DateTime from, DateTime to);

    Task<IReadOnlyList<AdminRecentActivityDto>> GetRecentActivityAsync(int limit);

    Task<IReadOnlyList<AdminLiveDriverDto>> GetLiveDriversAsync(bool onlineOnly, int limit);
}
