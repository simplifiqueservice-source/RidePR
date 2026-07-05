namespace RidePR.Application.DTOs;

public class AdminDashboardDto
{
    public DateTime GeneratedAt { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public AdminUsersOverviewDto Users { get; set; } = new();

    public AdminDriversOverviewDto Drivers { get; set; } = new();

    public AdminTripsOverviewDto Trips { get; set; } = new();

    public AdminPaymentsOverviewDto Payments { get; set; } = new();

    public AdminOperationsDto Operations { get; set; } = new();

    public IReadOnlyList<AdminRevenuePointDto> Revenue { get; set; } = Array.Empty<AdminRevenuePointDto>();

    public IReadOnlyList<AdminRecentActivityDto> RecentActivity { get; set; } = Array.Empty<AdminRecentActivityDto>();
}
