namespace RidePR.Application.DTOs;

public class CreateTripDto
{
    public Guid PassengerId { get; set; }

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    // Coordenadas da origem
    public double OriginLatitude { get; set; }

    public double OriginLongitude { get; set; }

    // Coordenadas do destino
    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }
}