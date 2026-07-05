using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class DispatchDecisionRequestDto
{
    [Required]
    public Guid TripId { get; set; }

    [Required]
    public Guid DriverId { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }
}
