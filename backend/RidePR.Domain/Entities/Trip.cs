using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; }

    public Guid PassengerId { get; set; }

    public Guid? DriverId { get; set; }

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    // Coordenadas
    public double OriginLatitude { get; set; }

    public double OriginLongitude { get; set; }

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    // Distância prevista
    public decimal EstimatedDistanceKm { get; set; }

    // Tempo previsto
    public decimal EstimatedDurationMinutes { get; set; }

    // Distância realmente rodada
    public decimal ActualDistanceKm { get; set; }

    public decimal Price { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Requested;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}