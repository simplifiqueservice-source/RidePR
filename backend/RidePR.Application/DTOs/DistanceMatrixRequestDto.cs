using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class DistanceMatrixRequestDto
{
    [MinLength(1)]
    public List<CoordinateDto> Origins { get; set; } = new();

    [MinLength(1)]
    public List<CoordinateDto> Destinations { get; set; } = new();

    public string? Provider { get; set; }
}
