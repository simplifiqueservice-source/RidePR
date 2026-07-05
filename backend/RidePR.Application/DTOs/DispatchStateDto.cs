namespace RidePR.Application.DTOs;

public class DispatchStateDto
{
    public Guid TripId { get; set; }

    public double RadiusKm { get; set; }

    public int TimeoutSeconds { get; set; }

    public int CurrentCandidateIndex { get; set; }

    public bool Completed { get; set; }

    public Guid? AcceptedDriverId { get; set; }

    public DispatchOfferDto? CurrentOffer { get; set; }

    public List<DispatchCandidateDto> Candidates { get; set; } = new();

    public List<DispatchRejectedDriverDto> RejectedDrivers { get; set; } = new();
}

public class DispatchRejectedDriverDto
{
    public Guid DriverId { get; set; }

    public string Reason { get; set; } = "";

    public DateTime RejectedAt { get; set; }
}
