namespace RidePR.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}