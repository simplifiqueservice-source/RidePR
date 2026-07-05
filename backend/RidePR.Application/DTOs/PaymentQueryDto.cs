using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class PaymentQueryDto
{
    public Guid? TripId { get; set; }

    public Guid? PassengerId { get; set; }

    public Guid? DriverId { get; set; }

    public PaymentMethod? Method { get; set; }

    public PaymentStatus? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
