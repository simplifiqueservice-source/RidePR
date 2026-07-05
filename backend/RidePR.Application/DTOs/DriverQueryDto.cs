using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class DriverQueryDto
{
    public string? Search { get; set; }

    public DriverStatus? Status { get; set; }

    public DriverApprovalStatus? ApprovalStatus { get; set; }

    public bool? Active { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
