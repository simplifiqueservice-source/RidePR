namespace RidePR.Application.DTOs;

public class VehicleResponseDto
{
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }

    public string DriverName { get; set; } = "";

    public string Plate { get; set; } = "";

    public string Model { get; set; } = "";

    public string Brand { get; set; } = "";

    public string Color { get; set; } = "";

    public int Year { get; set; }

    public string Renavam { get; set; } = "";

    public string Chassis { get; set; } = "";

    public Guid? CompanyId { get; set; }

    public bool Active { get; set; }

    public string? PhotoUrl { get; set; }

    public string? RegistrationDocumentUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
