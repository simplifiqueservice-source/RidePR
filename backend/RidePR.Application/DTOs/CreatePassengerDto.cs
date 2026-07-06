using System.ComponentModel.DataAnnotations;

namespace RidePR.Application.DTOs;

public class CreatePassengerDto
{
    [Required]
    public Guid UserId { get; set; }

    public Guid? BranchId { get; set; }

    [Required]
    public string Cpf { get; set; } = "";

    public DateTime BirthDate { get; set; }

    public string Phone { get; set; } = "";

    public string EmergencyPhone { get; set; } = "";

    public string Address { get; set; } = "";

    public string City { get; set; } = "";

    public string State { get; set; } = "";

    public string ZipCode { get; set; } = "";
}
