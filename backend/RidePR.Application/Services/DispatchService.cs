using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class DispatchService
{
    private readonly DriverLocationService _locationService;
    private readonly IDriverRepository _driverRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IDispatchQueue _queue;
    private readonly IDispatchNotifier _notifier;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly RouteService _routeService;
    private readonly IVehicleRepository _vehicleRepository;

    public DispatchService(
        DriverLocationService locationService,
        IDriverRepository driverRepository,
        ITripRepository tripRepository,
        IDispatchQueue queue,
        IDispatchNotifier notifier,
        IRealtimeNotifier realtimeNotifier,
        RouteService routeService,
        IVehicleRepository vehicleRepository)
    {
        _locationService = locationService;
        _driverRepository = driverRepository;
        _tripRepository = tripRepository;
        _queue = queue;
        _notifier = notifier;
        _realtimeNotifier = realtimeNotifier;
        _routeService = routeService;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<List<DriverLocation>> FindNearbyDriversAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var drivers = await _locationService.GetNearbyDriversAsync(
            latitude,
            longitude,
            radiusKm);

        return drivers
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();
    }

    public async Task<Result<List<DispatchCandidateDto>>> FindCandidatesAsync(DispatchNearbyQueryDto query)
    {
        var locations = await FindNearbyDriversAsync(query.Latitude, query.Longitude, query.RadiusKm);
        var candidates = await BuildCandidatesAsync(
            locations,
            query.Latitude,
            query.Longitude,
            Math.Max(1, query.MaxCandidates));

        return Result<List<DispatchCandidateDto>>.Ok(candidates);
    }

    public async Task<Result<DispatchStateDto>> StartDispatchAsync(DispatchRequestDto request)
    {
        var trip = await _tripRepository.GetByIdAsync(request.TripId);

        if (trip == null)
            return Result<DispatchStateDto>.Fail("Corrida nao encontrada.");

        if (trip.Status != TripStatus.Requested)
            return Result<DispatchStateDto>.Fail("Corrida nao esta aguardando motorista.");

        var locations = await FindNearbyDriversAsync(
            trip.OriginLatitude,
            trip.OriginLongitude,
            request.RadiusKm);

        var candidates = await BuildCandidatesAsync(
            locations,
            trip.OriginLatitude,
            trip.OriginLongitude,
            Math.Max(1, request.MaxCandidates));

        if (candidates.Count == 0)
            return Result<DispatchStateDto>.Fail("Nenhum motorista disponivel no raio informado.");

        var state = new DispatchStateDto
        {
            TripId = trip.Id,
            RadiusKm = request.RadiusKm,
            TimeoutSeconds = request.TimeoutSeconds,
            CurrentCandidateIndex = -1,
            Candidates = candidates
        };

        var assigned = await AssignNextDriverAsync(state, trip);

        if (!assigned)
            return Result<DispatchStateDto>.Fail("Nao foi possivel iniciar o despacho.");

        return Result<DispatchStateDto>.Ok(state);
    }

    public async Task<Result<DispatchStateDto>> GetStateAsync(Guid tripId)
    {
        var state = await _queue.GetAsync(tripId);

        if (state == null)
            return Result<DispatchStateDto>.Fail("Despacho nao encontrado.");

        return Result<DispatchStateDto>.Ok(state);
    }

    public async Task<Result<Trip>> AcceptAsync(Guid tripId, DispatchDriverDecisionDto dto)
    {
        var state = await _queue.GetAsync(tripId);

        if (state == null || state.CurrentOffer == null)
            return Result<Trip>.Fail("Oferta de despacho nao encontrada.");

        if (state.CurrentOffer.DriverId != dto.DriverId)
            return Result<Trip>.Fail("Oferta pertence a outro motorista.");

        if (DateTime.UtcNow > state.CurrentOffer.ExpiresAt)
        {
            await ExpireCurrentOfferAsync(state);
            return Result<Trip>.Fail("Oferta expirada.");
        }

        var trip = await _tripRepository.GetByIdAsync(tripId);

        if (trip == null)
            return Result<Trip>.Fail("Corrida nao encontrada.");

        if (trip.Status != TripStatus.Requested)
            return Result<Trip>.Fail("Corrida nao esta disponivel para aceite.");

        trip.DriverId = dto.DriverId;
        trip.Status = TripStatus.Accepted;

        await _tripRepository.UpdateAsync(trip);

        var driver = await _driverRepository.GetByIdAsync(dto.DriverId);

        if (driver != null)
        {
            driver.Status = DriverStatus.Busy;
            driver.UpdatedAt = DateTime.UtcNow;
            await _driverRepository.UpdateAsync(driver);
        }

        await _tripRepository.SaveChangesAsync();
        await _driverRepository.SaveChangesAsync();

        state.Completed = true;
        state.AcceptedDriverId = dto.DriverId;
        await _queue.SetAsync(state);
        await _notifier.NotifyDispatchUpdatedAsync(state);
        await _realtimeNotifier.NotifyTripAcceptedAsync(trip);
        await _queue.RemoveAsync(tripId);

        return Result<Trip>.Ok(trip);
    }

    public async Task<Result<DispatchStateDto>> RejectAsync(Guid tripId, DispatchDriverDecisionDto dto)
    {
        var state = await _queue.GetAsync(tripId);

        if (state == null || state.CurrentOffer == null)
            return Result<DispatchStateDto>.Fail("Oferta de despacho nao encontrada.");

        if (state.CurrentOffer.DriverId != dto.DriverId)
            return Result<DispatchStateDto>.Fail("Oferta pertence a outro motorista.");

        state.RejectedDrivers.Add(new DispatchRejectedDriverDto
        {
            DriverId = dto.DriverId,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? "Recusado pelo motorista." : dto.Reason,
            RejectedAt = DateTime.UtcNow
        });

        var trip = await _tripRepository.GetByIdAsync(tripId);

        if (trip == null)
            return Result<DispatchStateDto>.Fail("Corrida nao encontrada.");

        var reassigned = await AssignNextDriverAsync(state, trip);

        if (!reassigned)
        {
            state.Completed = true;
            state.CurrentOffer = null;
            await _queue.SetAsync(state);
            await _notifier.NotifyDispatchUpdatedAsync(state);
            return Result<DispatchStateDto>.Fail("Todos os motoristas recusaram ou expiraram.");
        }

        return Result<DispatchStateDto>.Ok(state);
    }

    public async Task<Result<DispatchStateDto>> ReassignAsync(Guid tripId)
    {
        var state = await _queue.GetAsync(tripId);

        if (state == null)
            return Result<DispatchStateDto>.Fail("Despacho nao encontrado.");

        var trip = await _tripRepository.GetByIdAsync(tripId);

        if (trip == null)
            return Result<DispatchStateDto>.Fail("Corrida nao encontrada.");

        await ExpireCurrentOfferAsync(state);

        var reassigned = await AssignNextDriverAsync(state, trip);

        if (!reassigned)
            return Result<DispatchStateDto>.Fail("Nao ha motoristas restantes para reatribuir.");

        return Result<DispatchStateDto>.Ok(state);
    }

    public async Task ProcessTimeoutsAsync()
    {
        var tripIds = await _queue.GetActiveTripIdsAsync();

        foreach (var tripId in tripIds)
        {
            var state = await _queue.GetAsync(tripId);

            if (state?.CurrentOffer == null || state.Completed)
                continue;

            if (DateTime.UtcNow <= state.CurrentOffer.ExpiresAt)
                continue;

            var trip = await _tripRepository.GetByIdAsync(tripId);

            if (trip == null || trip.Status != TripStatus.Requested)
            {
                await _queue.RemoveAsync(tripId);
                continue;
            }

            await ExpireCurrentOfferAsync(state);
            var reassigned = await AssignNextDriverAsync(state, trip);

            if (!reassigned)
            {
                state.Completed = true;
                state.CurrentOffer = null;
                await _queue.SetAsync(state);
                await _notifier.NotifyDispatchUpdatedAsync(state);
            }
        }
    }

    private async Task<bool> AssignNextDriverAsync(DispatchStateDto state, Trip trip)
    {
        while (state.CurrentCandidateIndex + 1 < state.Candidates.Count)
        {
            state.CurrentCandidateIndex++;
            var candidate = state.Candidates[state.CurrentCandidateIndex];

            if (state.RejectedDrivers.Any(x => x.DriverId == candidate.DriverId))
                continue;

            var driver = await _driverRepository.GetByIdAsync(candidate.DriverId);

            if (driver == null ||
                !driver.Active ||
                driver.Status != DriverStatus.Online ||
                driver.ApprovalStatus != DriverApprovalStatus.Approved ||
                !await HasActiveVehicleAsync(driver.Id) ||
                await HasActiveTripAsync(driver.Id) ||
                (trip.BranchId.HasValue &&
                 driver.BranchId.HasValue &&
                 driver.BranchId.Value != trip.BranchId.Value))
                continue;

            var now = DateTime.UtcNow;
            state.CurrentOffer = new DispatchOfferDto
            {
                TripId = trip.Id,
                DriverId = candidate.DriverId,
                OfferedAt = now,
                ExpiresAt = now.AddSeconds(state.TimeoutSeconds),
                DistanceKm = candidate.DistanceKm,
                EtaMinutes = candidate.EtaMinutes,
                Price = trip.Price,
                Origin = trip.Origin,
                Destination = trip.Destination
            };

            await _queue.SetAsync(state);
            await _notifier.NotifyOfferAsync(candidate.DriverId, state.CurrentOffer);
            await _notifier.NotifyDispatchUpdatedAsync(state);

            return true;
        }

        return false;
    }

    private async Task ExpireCurrentOfferAsync(DispatchStateDto state)
    {
        if (state.CurrentOffer == null)
            return;

        var offer = state.CurrentOffer;

        state.RejectedDrivers.Add(new DispatchRejectedDriverDto
        {
            DriverId = offer.DriverId,
            Reason = "Timeout.",
            RejectedAt = DateTime.UtcNow
        });

        await _notifier.NotifyOfferExpiredAsync(offer.DriverId, state.TripId);
    }

    private async Task<List<DispatchCandidateDto>> BuildCandidatesAsync(
        List<DriverLocation> locations,
        double originLatitude,
        double originLongitude,
        int maxCandidates)
    {
        var candidates = new List<DispatchCandidateDto>();

        foreach (var location in locations)
        {
            var driver = await _driverRepository.GetByIdAsync(location.DriverId);

            if (driver == null ||
                !driver.Active ||
                driver.Status != DriverStatus.Online ||
                driver.ApprovalStatus != DriverApprovalStatus.Approved ||
                !await HasActiveVehicleAsync(driver.Id) ||
                await HasActiveTripAsync(driver.Id))
                continue;

            var distanceKm = CalculateDistanceKm(
                location.Position.Y,
                location.Position.X,
                originLatitude,
                originLongitude);

            var etaMinutes = await CalculateEtaAsync(
                location.Position.Y,
                location.Position.X,
                originLatitude,
                originLongitude,
                distanceKm);

            candidates.Add(new DispatchCandidateDto
            {
                DriverId = location.DriverId,
                DriverName = driver.User?.Name ?? "",
                Latitude = location.Position.Y,
                Longitude = location.Position.X,
                DistanceKm = distanceKm,
                EtaMinutes = etaMinutes,
                LocationUpdatedAt = location.UpdatedAt
            });
        }

        return candidates
            .OrderBy(x => x.EtaMinutes)
            .ThenBy(x => x.DistanceKm)
            .Take(maxCandidates)
            .ToList();
    }

    private async Task<decimal> CalculateEtaAsync(
        double driverLatitude,
        double driverLongitude,
        double originLatitude,
        double originLongitude,
        decimal fallbackDistanceKm)
    {
        var result = await _routeService.GetRouteAsync(new MapRouteRequestDto
        {
            Origin = new CoordinateDto { Latitude = driverLatitude, Longitude = driverLongitude },
            Destination = new CoordinateDto { Latitude = originLatitude, Longitude = originLongitude }
        });

        if (result.Success && result.Data != null)
            return result.Data.EtaMinutes;

        const decimal averageUrbanSpeedKmH = 28m;

        return fallbackDistanceKm / averageUrbanSpeedKmH * 60m;
    }

    private static decimal CalculateDistanceKm(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        const double earthRadiusKm = 6371;
        var dLat = ToRadians(destinationLat - originLat);
        var dLng = ToRadians(destinationLng - originLng);
        var lat1 = ToRadians(originLat);
        var lat2 = ToRadians(destinationLat);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(earthRadiusKm * c);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private async Task<bool> HasActiveVehicleAsync(Guid driverId)
    {
        var vehicles = await _vehicleRepository.GetPagedAsync(null, driverId, true, 1, 1);
        return vehicles.Count > 0;
    }

    private async Task<bool> HasActiveTripAsync(Guid driverId)
    {
        var trips = await _tripRepository.GetAllAsync();
        return trips.Any(x =>
            x.DriverId == driverId &&
            x.Status is TripStatus.Accepted or TripStatus.InProgress);
    }
}
