namespace RidePR.Application.DTOs;

public class AdminOperationsDto
{
    public int PendingDriverApprovals { get; set; }

    public int ActiveTrips { get; set; }

    public int PendingPayments { get; set; }

    public int PendingRefunds { get; set; }

    public int OnlineDrivers { get; set; }

    public int AvailableDrivers { get; set; }
}
