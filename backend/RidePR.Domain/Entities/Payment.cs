using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public Trip Trip { get; set; } = null!;

    public Guid PassengerId { get; set; }

    public Passenger Passenger { get; set; } = null!;

    public Guid? DriverId { get; set; }

    public Driver? Driver { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public decimal RefundedAmount { get; set; }

    public string Currency { get; set; } = "BRL";

    public string? Provider { get; set; }

    public string? ProviderPaymentId { get; set; }

    public string? PixQrCode { get; set; }

    public string? PixCopyPaste { get; set; }

    public DateTime? PixExpiresAt { get; set; }

    public string? CardLast4 { get; set; }

    public string? CardBrand { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<PaymentSplit> Splits { get; set; } = new List<PaymentSplit>();

    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
}
