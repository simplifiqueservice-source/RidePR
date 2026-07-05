namespace RidePR.Application.DTOs;

public class DistanceMatrixResultDto
{
    public string Provider { get; set; } = "";

    public List<DistanceMatrixElementDto> Elements { get; set; } = new();
}

public class DistanceMatrixElementDto
{
    public int OriginIndex { get; set; }

    public int DestinationIndex { get; set; }

    public decimal DistanceKm { get; set; }

    public decimal DurationMinutes { get; set; }
}
