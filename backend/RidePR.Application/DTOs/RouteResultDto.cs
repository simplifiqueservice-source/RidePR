namespace RidePR.Application.DTOs;

public class RouteResultDto
{
    public decimal DistanceKm { get; set; }

    public decimal DurationMinutes { get; set; }

    public decimal EtaMinutes { get; set; }

    public string Geometry { get; set; } = string.Empty;

    public string GeometryFormat { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;
}
