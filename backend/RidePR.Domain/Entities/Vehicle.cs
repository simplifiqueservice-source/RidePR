namespace RidePR.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }

    public Driver Driver { get; set; } = null!;

    public string Plate { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Renavam { get; set; } = string.Empty;

    public string Chassis { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? PhotoUrl { get; set; }

    public string? RegistrationDocumentUrl { get; set; }
}
