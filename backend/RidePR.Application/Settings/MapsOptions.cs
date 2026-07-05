namespace RidePR.Application.Settings;

public class MapsOptions
{
    public string DefaultProvider { get; set; } = "OpenStreetMap";

    public int CacheMinutes { get; set; } = 15;

    public int MaxMatrixCoordinates { get; set; } = 25;

    public GoogleMapsOptions Google { get; set; } = new();

    public OpenStreetMapOptions OpenStreetMap { get; set; } = new();
}

public class GoogleMapsOptions
{
    public string ApiKey { get; set; } = "";

    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api";

    public int TimeoutSeconds { get; set; } = 10;
}

public class OpenStreetMapOptions
{
    public string OsrmBaseUrl { get; set; } = "https://router.project-osrm.org";

    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    public string UserAgent { get; set; } = "RidePR/1.0";

    public string AcceptLanguage { get; set; } = "pt-BR";

    public string RouteGeometryFormat { get; set; } = "polyline";

    public int TimeoutSeconds { get; set; } = 10;
}
