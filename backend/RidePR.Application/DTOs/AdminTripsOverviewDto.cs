namespace RidePR.Application.DTOs;

public class AdminTripsOverviewDto
{
    public int TotalTrips { get; set; }

    public int RequestedTrips { get; set; }

    public int AcceptedTrips { get; set; }

    public int InProgressTrips { get; set; }

    public int FinishedTrips { get; set; }

    public int CancelledTrips { get; set; }

    public decimal CompletionRate { get; set; }
}
