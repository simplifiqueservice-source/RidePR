using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class PaymentRefundResponseDto
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = "";

    public RefundStatus Status { get; set; }

    public string? ProviderRefundId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
