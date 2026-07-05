namespace RidePR.Application.DTOs;

public class PassengerQueryDto
{
    public string? Search { get; set; }

    public bool? Active { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
