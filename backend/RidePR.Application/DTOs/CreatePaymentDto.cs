using System.ComponentModel.DataAnnotations;
using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class CreatePaymentDto
{
    [Required]
    public Guid TripId { get; set; }

    [Required]
    public Guid PassengerId { get; set; }

    public Guid? DriverId { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    public string? Provider { get; set; }

    public CardPaymentDto? Card { get; set; }
}
