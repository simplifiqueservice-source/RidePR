using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
