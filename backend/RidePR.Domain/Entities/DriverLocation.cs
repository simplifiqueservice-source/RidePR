using NetTopologySuite.Geometries;

namespace RidePR.Domain.Entities;

public class DriverLocation
{
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }

    // Localização geográfica (PostGIS)
    public Point Position { get; set; } = default!;

    public double Speed { get; set; }

    public double Heading { get; set; }

    public bool Online { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}