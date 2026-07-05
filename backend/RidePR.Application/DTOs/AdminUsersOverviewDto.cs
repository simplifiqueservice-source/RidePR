namespace RidePR.Application.DTOs;

public class AdminUsersOverviewDto
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int NewUsers { get; set; }

    public int Administrators { get; set; }
}
