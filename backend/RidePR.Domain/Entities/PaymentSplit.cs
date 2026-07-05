namespace RidePR.Domain.Entities;

public class PaymentSplit
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public Guid? DriverId { get; set; }

    public Driver? Driver { get; set; }

    public string RecipientType { get; set; } = "";

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }

    public bool Settled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SettledAt { get; set; }
}
