using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class DispatchDriverDecisionDto
{
    [Required]
    public Guid DriverId { get; set; }

    public string? Reason { get; set; }
}
