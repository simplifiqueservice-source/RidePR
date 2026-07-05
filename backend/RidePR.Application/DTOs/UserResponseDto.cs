using RidePR.Domain.Enums;

namespace RidePR.Application.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string RoleName => Role.ToString();

    public bool Active { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
