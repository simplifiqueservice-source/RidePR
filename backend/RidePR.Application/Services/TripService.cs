using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Application.Services;

public class TripService
{
    private readonly ITripRepository _repo;
    private readonly RouteService _routeService;
    private readonly FareCalculatorService _fareCalculator;
    private readonly IDriverRepository _driverRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public TripService(
        ITripRepository repo,
        RouteService routeService,
        FareCalculatorService fareCalculator,
        IDriverRepository driverRepository,
        IPassengerRepository passengerRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _repo = repo;
        _routeService = routeService;
        _fareCalculator = fareCalculator;
        _driverRepository = driverRepository;
        _passengerRepository = passengerRepository;
        _realtimeNotifier = realtimeNotifier;
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

        var passenger = await _passengerRepository.GetByIdAsync(dto.PassengerId);
        var branchId = dto.BranchId ?? passenger?.BranchId;

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            PassengerId = dto.PassengerId,
            BranchId = branchId,
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
                : await CalculateFareAsync(route.DistanceKm, route.DurationMinutes, branchId),
            DriverId = null
        };

        await _repo.AddAsync(trip);
        await _repo.SaveChangesAsync();
        await _realtimeNotifier.NotifyTripRequestedAsync(trip);

        return trip;
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<List<Trip>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
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
        await _realtimeNotifier.NotifyTripAcceptedAsync(trip);

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
        await _realtimeNotifier.NotifyTripStartedAsync(trip);

        return trip;
    }

    // =========================
    // FINALIZAR CORRIDA
    // =========================
    public async Task<Trip?> FinishTripAsync(
        Guid tripId,
        Guid driverId,
        double? actualDistanceKm,
        decimal? actualDurationMinutes)
    {
        var trip = await _repo.GetByIdAsync(tripId);

        if (trip == null)
            return null;

        if (trip.Status != TripStatus.InProgress)
            return null;

        if (trip.DriverId != driverId)
            return null;

        trip.Status = TripStatus.Finished;
        trip.ActualDistanceKm = actualDistanceKm.HasValue && actualDistanceKm.Value > 0
            ? (decimal)actualDistanceKm.Value
            : trip.EstimatedDistanceKm;

        var durationMinutes = actualDurationMinutes.HasValue && actualDurationMinutes.Value > 0
            ? actualDurationMinutes.Value
            : trip.EstimatedDurationMinutes;

        trip.Price = await CalculateFareAsync(trip.ActualDistanceKm, durationMinutes, trip.BranchId);

        var driver = await _driverRepository.GetByIdAsync(driverId);

        if (driver != null)
        {
            driver.Status = DriverStatus.Online;
            driver.UpdatedAt = DateTime.UtcNow;
            await _driverRepository.UpdateAsync(driver);
        }

        await _repo.SaveChangesAsync();
        await _driverRepository.SaveChangesAsync();
        await _realtimeNotifier.NotifyTripFinishedAsync(trip);

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

    private async Task<decimal> CalculateFareAsync(decimal distanceKm, decimal durationMinutes)
    {
        return await CalculateFareAsync(distanceKm, durationMinutes, null);
    }

    private async Task<decimal> CalculateFareAsync(
        decimal distanceKm,
        decimal durationMinutes,
        Guid? branchId)
    {
        try
        {
            return await _fareCalculator.CalculateAsync(distanceKm, durationMinutes, branchId);
        }
        catch
        {
            return CalculateByKmPricing((double)distanceKm);
        }
    }
}
