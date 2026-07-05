namespace RidePR.Application.DTOs;

public class GeocodingResultDto
{
    public string Address { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Provider { get; set; } = "";
}
