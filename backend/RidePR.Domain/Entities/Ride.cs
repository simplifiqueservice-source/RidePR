namespace RidePR.Domain.Entities;

public enum RideStatus
{
    Requested = 0,
    Accepted = 1,
    DriverArrived = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public class Ride
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PassengerId { get; set; }

    public Passenger? Passenger { get; set; }

    public Guid? DriverId { get; set; }

    public Driver? Driver { get; set; }

    public string OriginAddress { get; set; } = "";

    public double OriginLatitude { get; set; }

    public double OriginLongitude { get; set; }

    public string DestinationAddress { get; set; } = "";

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    public decimal EstimatedPrice { get; set; }

    public decimal? FinalPrice { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Requested;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcceptedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }
}