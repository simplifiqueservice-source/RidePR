using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;

namespace RidePR.Infrastructure.Payments;

public class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayResultDto> CreatePixAsync(Payment payment)
    {
        var providerId = $"pix_{Guid.NewGuid():N}";

        return Task.FromResult(new PaymentGatewayResultDto
        {
            Success = true,
            ProviderPaymentId = providerId,
            PixQrCode = $"ridepr-pix://pay/{providerId}",
            PixCopyPaste = $"000201-RIDEPR-{providerId}-{payment.Amount:0.00}",
            PixExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
    }

    public Task<PaymentGatewayResultDto> ChargeCardAsync(Payment payment, CardPaymentDto card)
    {
        if (string.IsNullOrWhiteSpace(card.Token))
        {
            return Task.FromResult(new PaymentGatewayResultDto
            {
                Success = false,
                FailureReason = "Token do cartao nao informado."
            });
        }

        return Task.FromResult(new PaymentGatewayResultDto
        {
            Success = true,
            ProviderPaymentId = $"card_{Guid.NewGuid():N}"
        });
    }

    public Task<PaymentRefundGatewayResultDto> RefundAsync(Payment payment, decimal amount, string reason)
    {
        return Task.FromResult(new PaymentRefundGatewayResultDto
        {
            Success = true,
            ProviderRefundId = $"refund_{Guid.NewGuid():N}"
        });
    }
}
