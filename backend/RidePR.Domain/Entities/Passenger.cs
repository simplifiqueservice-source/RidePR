namespace RidePR.Domain.Entities;

public class Passenger
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = "";

    public string CPF { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Email { get; set; } = "";

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}