namespace RidePR.Application.DTOs;

public class PaymentGatewayResultDto
{
    public bool Success { get; set; }

    public string? ProviderPaymentId { get; set; }

    public string? PixQrCode { get; set; }

    public string? PixCopyPaste { get; set; }

    public DateTime? PixExpiresAt { get; set; }

    public string? FailureReason { get; set; }
}

public class PaymentRefundGatewayResultDto
{
    public bool Success { get; set; }

    public string? ProviderRefundId { get; set; }

    public string? FailureReason { get; set; }
}
