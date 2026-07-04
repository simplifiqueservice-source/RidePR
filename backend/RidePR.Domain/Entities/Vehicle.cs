namespace RidePR.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }

    public string Plate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;

    public int Year { get; set; }

    public Guid CompanyId { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}