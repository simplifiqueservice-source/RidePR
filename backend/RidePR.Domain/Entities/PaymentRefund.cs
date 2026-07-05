using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class PaymentRefund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Reason { get; set; } = "";

    public RefundStatus Status { get; set; }

    public string? ProviderRefundId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
