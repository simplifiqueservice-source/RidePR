using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class PaymentResponseDto
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public Guid PassengerId { get; set; }

    public Guid? DriverId { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public decimal RefundedAmount { get; set; }

    public string Currency { get; set; } = "";

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

    public List<PaymentSplitResponseDto> Splits { get; set; } = new();

    public List<PaymentRefundResponseDto> Refunds { get; set; } = new();
}
