namespace RidePR.Domain.Entities;

public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string City { get; set; } = "";

    public string State { get; set; } = "";

    public string Address { get; set; } = "";

    public string Phone { get; set; } = "";

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
