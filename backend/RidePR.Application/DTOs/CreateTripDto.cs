using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class CreateTripDto
{
    [Required]
    public Guid PassengerId { get; set; }

    [Required]
    [StringLength(300)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Destination { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double OriginLatitude { get; set; }

    [Range(-180, 180)]
    public double OriginLongitude { get; set; }

    [Range(-90, 90)]
    public double DestinationLatitude { get; set; }

    [Range(-180, 180)]
    public double DestinationLongitude { get; set; }
}
