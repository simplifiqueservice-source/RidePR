namespace RidePR.Application.DTOs;

public class VehicleQueryDto
{
    public string? Search { get; set; }

    public Guid? DriverId { get; set; }

    public bool? Active { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
