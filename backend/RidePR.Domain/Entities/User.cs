using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Driver? Driver { get; set; }

    // Perfil do usuário
    public UserRole Role { get; set; } = UserRole.Passenger;

    // Conta ativa?
    public bool Active { get; set; } = true;

    // Último acesso
    public DateTime? LastLoginAt { get; set; }

    // Data de criação
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Tokens de atualização
    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}