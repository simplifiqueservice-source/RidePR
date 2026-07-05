using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class UpdateUserDto
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool Active { get; set; }
}
