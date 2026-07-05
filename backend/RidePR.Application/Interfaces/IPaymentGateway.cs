using RidePR.Application.DTOs;
using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentGatewayResultDto> CreatePixAsync(Payment payment);

    Task<PaymentGatewayResultDto> ChargeCardAsync(Payment payment, CardPaymentDto card);

    Task<PaymentRefundGatewayResultDto> RefundAsync(Payment payment, decimal amount, string reason);
}
