namespace RidePR.Application.DTOs;

public class EtaResultDto
{
    public decimal DistanceKm { get; set; }

    public decimal EtaMinutes { get; set; }

    public string Provider { get; set; } = "";
}
