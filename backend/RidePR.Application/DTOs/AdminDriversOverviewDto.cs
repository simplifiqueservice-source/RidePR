namespace RidePR.Application.DTOs;

public class AdminDriversOverviewDto
{
    public int TotalDrivers { get; set; }

    public int ActiveDrivers { get; set; }

    public int PendingApproval { get; set; }

    public int ApprovedDrivers { get; set; }

    public int OnlineDrivers { get; set; }

    public int BusyDrivers { get; set; }
}
