namespace RidePR.Application.DTOs;

public class PaymentSplitResponseDto
{
    public Guid Id { get; set; }

    public Guid? DriverId { get; set; }

    public string RecipientType { get; set; } = "";

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }

    public bool Settled { get; set; }
}
