namespace RidePR.Application.DTOs;

public class PassengerHistoryResponseDto
{
    public Guid Id { get; set; }

    public Guid PassengerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
