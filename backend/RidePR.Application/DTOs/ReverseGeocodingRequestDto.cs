using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class ReverseGeocodingRequestDto
{
    [Required]
    public CoordinateDto Coordinate { get; set; } = new();

    public string? Provider { get; set; }
}
