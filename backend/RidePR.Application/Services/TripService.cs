using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Application.Services;

public class TripService
{
    private readonly ITripRepository _repo;
    private readonly RouteService _routeService;

    public TripService(
        ITripRepository repo,
        RouteService routeService)
    {
        _repo = repo;
        _routeService = routeService;
    }

    // =========================
    // CRIAR CORRIDA
    // =========================
    public async Task<Trip> CreateAsync(CreateTripDto dto)
    {
        var route = await _routeService.GetRouteAsync(
            dto.OriginLatitude,
            dto.OriginLongitude,
            dto.DestinationLatitude,
            dto.DestinationLongitude);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            PassengerId = dto.PassengerId,
            Origin = dto.Origin,
            Destination = dto.Destination,
            OriginLatitude = dto.OriginLatitude,
            OriginLongitude = dto.OriginLongitude,
            DestinationLatitude = dto.DestinationLatitude,
            DestinationLongitude = dto.DestinationLongitude,
            EstimatedDistanceKm = route?.DistanceKm ?? 0,
            EstimatedDurationMinutes = route?.DurationMinutes ?? 0,
            Status = TripStatus.Requested,
            CreatedAt = DateTime.UtcNow,
            Price = route == null
                ? 0
                : CalculateByKmPricing((double)route.DistanceKm),
            DriverId = null
        };

        await _repo.AddAsync(trip);
        await _repo.SaveChangesAsync();

        return trip;
    }

    // =========================
    // ACEITAR CORRIDA
    // =========================
    public async Task<Trip?> AcceptAsync(Guid tripId, Guid driverId)
    {
        var trip = await _repo.GetByIdAsync(tripId);

        if (trip == null)
            return null;

        if (trip.Status != TripStatus.Requested)
            return null;

        trip.DriverId = driverId;
        trip.Status = TripStatus.Accepted;

        await _repo.SaveChangesAsync();

        return trip;
    }

    // =========================
    // INICIAR CORRIDA
    // =========================
    public async Task<Trip?> StartTripAsync(Guid tripId, Guid driverId)
    {
        var trip = await _repo.GetByIdAsync(tripId);

        if (trip == null)
            return null;

        if (trip.Status != TripStatus.Accepted)
            return null;

        if (trip.DriverId != driverId)
            return null;

        trip.Status = TripStatus.InProgress;

        await _repo.SaveChangesAsync();

        return trip;
    }

    // =========================
    // FINALIZAR CORRIDA
    // =========================
    public async Task<Trip?> FinishTripAsync(Guid tripId, Guid driverId, double distanceKm)
    {
        var trip = await _repo.GetByIdAsync(tripId);

        if (trip == null)
            return null;

        if (trip.Status != TripStatus.InProgress)
            return null;

        if (trip.DriverId != driverId)
            return null;

        trip.Status = TripStatus.Finished;

        trip.Price = CalculateByKmPricing(distanceKm);

        await _repo.SaveChangesAsync();

        return trip;
    }

    // =========================
    // REGRA DE COBRANÇA (KM)
    // =========================
    private decimal CalculateByKmPricing(double distanceKm)
    {
        const decimal basePrice = 7m;   // valor mínimo
        const decimal pricePerKm = 1m;  // por km
        const double minKm = 1.0;

        if (distanceKm <= minKm)
            return basePrice;

        var extraKm = (decimal)(distanceKm - minKm);

        return basePrice + (extraKm * pricePerKm);
    }
}
