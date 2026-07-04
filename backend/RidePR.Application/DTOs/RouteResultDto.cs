namespace RidePR.Application.DTOs;

public class RouteResultDto
{
    public decimal DistanceKm { get; set; }

    public decimal DurationMinutes { get; set; }

    public string Geometry { get; set; } = string.Empty;
}