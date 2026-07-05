using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class UserQueryDto
{
    public string? Search { get; set; }

    public UserRole? Role { get; set; }

    public bool? Active { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
