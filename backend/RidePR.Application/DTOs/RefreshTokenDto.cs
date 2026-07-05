using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class RefreshTokenDto
{
    [Required]
    [StringLength(512, MinimumLength = 32)]
    public string RefreshToken { get; set; } = string.Empty;
}
