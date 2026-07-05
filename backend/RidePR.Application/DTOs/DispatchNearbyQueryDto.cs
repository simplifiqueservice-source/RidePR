using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class DispatchNearbyQueryDto
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0.1, 100)]
    public double RadiusKm { get; set; } = 5;

    public int MaxCandidates { get; set; } = 10;
}
