namespace RidePR.Application.DTOs;

public class DispatchCandidateDto
{
    public Guid DriverId { get; set; }

    public string DriverName { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public decimal DistanceKm { get; set; }

    public decimal EtaMinutes { get; set; }

    public DateTime LocationUpdatedAt { get; set; }
}
