namespace RidePR.Application.DTOs;

public class AdminRecentActivityDto
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    public DateTime CreatedAt { get; set; }
}
