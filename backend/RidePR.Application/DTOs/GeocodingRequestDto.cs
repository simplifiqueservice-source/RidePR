using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class GeocodingRequestDto
{
    [Required]
    public string Address { get; set; } = "";

    public string? Provider { get; set; }
}
