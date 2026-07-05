using System.ComponentModel.DataAnnotations;
using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class UpdateDriverStatusDto
{
    [Required]
    public DriverStatus Status { get; set; }
}
