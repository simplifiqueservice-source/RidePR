using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class StartTripDto
{
    [Required]
    public Guid DriverId { get; set; }
}
