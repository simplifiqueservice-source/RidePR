using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class CoordinateDto
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}
