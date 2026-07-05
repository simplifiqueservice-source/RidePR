using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class PassengerHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PassengerId { get; set; }

    public Passenger Passenger { get; set; } = null!;

    public PassengerHistoryType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
