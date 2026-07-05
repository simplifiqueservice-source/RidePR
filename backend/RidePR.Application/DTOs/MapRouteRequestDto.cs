using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class MapRouteRequestDto
{
    [Required]
    public CoordinateDto Origin { get; set; } = new();

    [Required]
    public CoordinateDto Destination { get; set; } = new();

    public string? GeometryFormat { get; set; }

    public string? Provider { get; set; }
}
