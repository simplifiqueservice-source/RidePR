using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class ChangeUserPasswordDto
{
    [Required]
    [MinLength(8)]
    [StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
