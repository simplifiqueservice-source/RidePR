using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class DispatchRequestDto
{
    [Required]
    public Guid TripId { get; set; }

    [Range(0.1, 100)]
    public double RadiusKm { get; set; } = 5;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    public int MaxCandidates { get; set; } = 10;
}
