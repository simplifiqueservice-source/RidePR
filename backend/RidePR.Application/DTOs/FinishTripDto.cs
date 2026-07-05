using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class FinishTripDto
{
    [Required]
    public Guid DriverId { get; set; }

    [Range(0, 9999)]
    public double? ActualDistanceKm { get; set; }

    [Range(0, 99999)]
    public decimal? ActualDurationMinutes { get; set; }
}
