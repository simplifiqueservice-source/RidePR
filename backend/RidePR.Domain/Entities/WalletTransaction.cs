using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Wallet Wallet { get; set; } = null!;

    public Guid? PaymentId { get; set; }

    public Payment? Payment { get; set; }

    public WalletTransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
