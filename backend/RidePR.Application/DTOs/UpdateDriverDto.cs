namespace RidePR.Application.DTOs;

public class UpdateDriverDto
{
    public Guid? BranchId { get; set; }

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

    public bool Active { get; set; } = true;
}
