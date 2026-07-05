using System.ComponentModel.DataAnnotations;
using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class DriverApprovalDto
{
    [Required]
    public DriverApprovalStatus ApprovalStatus { get; set; }

    public string? RejectReason { get; set; }
}
