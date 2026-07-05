namespace RidePR.Application.DTOs;

public class WalletResponseDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Balance { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }
}
