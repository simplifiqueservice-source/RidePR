namespace RidePR.Application.DTOs;

public class DispatchOfferDto
{
    public Guid TripId { get; set; }

    public Guid DriverId { get; set; }

    public DateTime OfferedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public decimal DistanceKm { get; set; }

    public decimal EtaMinutes { get; set; }

    public decimal Price { get; set; }

    public string Origin { get; set; } = "";

    public string Destination { get; set; } = "";
}
