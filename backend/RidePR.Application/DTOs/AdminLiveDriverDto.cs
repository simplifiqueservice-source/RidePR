namespace RidePR.Application.DTOs;

public class AdminLiveDriverDto
{
    public Guid DriverId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ApprovalStatus { get; set; } = string.Empty;

    public bool Online { get; set; }

    public Guid? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    public string Vehicle { get; set; } = string.Empty;

    public string Plate { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Speed { get; set; }

    public double Heading { get; set; }

    public DateTime UpdatedAt { get; set; }
}
