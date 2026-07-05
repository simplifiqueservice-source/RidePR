using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class CreateVehicleDto
{
    [Required]
    public Guid DriverId { get; set; }

    [Required]
    public string Plate { get; set; } = "";

    [Required]
    public string Model { get; set; } = "";

    [Required]
    public string Brand { get; set; } = "";

    public string Color { get; set; } = "";

    [Range(1900, 2100)]
    public int Year { get; set; }

    public string Renavam { get; set; } = "";

    public string Chassis { get; set; } = "";

    public Guid? CompanyId { get; set; }
}
