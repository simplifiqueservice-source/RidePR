using RidePR.Domain.Enums;

namespace RidePR.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public string Cpf { get; set; } = "";

    public string Rg { get; set; } = "";

    public DateTime BirthDate { get; set; }

    public string Phone { get; set; } = "";

    public string EmergencyPhone { get; set; } = "";

    public string Address { get; set; } = "";

    public string City { get; set; } = "";

    public string State { get; set; } = "";

    public string ZipCode { get; set; } = "";

    public string CnhNumber { get; set; } = "";

    public string CnhCategory { get; set; } = "";

    public DateTime CnhExpiration { get; set; }

    public DriverStatus Status { get; set; }

    public DriverApprovalStatus ApprovalStatus { get; set; }

    public string? RejectReason { get; set; }

    public bool Active { get; set; }

    public string? PhotoUrl { get; set; }

    public string? CnhFrontUrl { get; set; }

    public string? CnhBackUrl { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
