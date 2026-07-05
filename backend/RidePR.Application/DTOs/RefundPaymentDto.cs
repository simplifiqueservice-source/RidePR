using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class RefundPaymentDto
{
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    [Required]
    public string Reason { get; set; } = "";
}
