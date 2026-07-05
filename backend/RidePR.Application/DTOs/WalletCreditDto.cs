using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class WalletCreditDto
{
    [Required]
    public Guid UserId { get; set; }

    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    public string Description { get; set; } = "Credito em carteira.";
}
