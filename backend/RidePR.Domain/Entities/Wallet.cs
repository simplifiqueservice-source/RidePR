namespace RidePR.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public decimal Balance { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
