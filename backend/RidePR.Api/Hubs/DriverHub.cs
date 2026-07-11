using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using RidePR.Api.Services;
using RidePR.Application.Interfaces;
using RidePR.Application.Services;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Hubs;

[Authorize(Roles = "Administrator,Driver,Passenger")]
public class DriverHub : Hub
{
    private readonly DriverLocationService _locationService;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ApplicationDbContext _context;

    public DriverHub(
        DriverLocationService locationService,
        IRealtimeNotifier realtimeNotifier,
        ApplicationDbContext context)
    {
        _locationService = locationService;
        _realtimeNotifier = realtimeNotifier;
        _context = context;
    }

    public async Task<object> JoinDriverGroup(Guid driverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRDispatchNotifier.GetDriverGroup(driverId));
        return new
        {
            Event = "DRIVER_GROUP_JOINED",
            DriverId = driverId,
            Group = SignalRDispatchNotifier.GetDriverGroup(driverId),
            Context.ConnectionId
        };
    }

    public async Task LeaveDriverGroup(Guid driverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRDispatchNotifier.GetDriverGroup(driverId));
    }

    public async Task UpdateLocation(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        await ValidateDriverLocationUpdateAsync(driverId, latitude, longitude);

        await _locationService.UpdateLocationAsync(
            driverId,
            latitude,
            longitude,
            speed,
            heading);

        await _realtimeNotifier.NotifyDriverLocationUpdatedAsync(
            driverId,
            latitude,
            longitude,
            speed,
            heading);
    }

    private async Task ValidateDriverLocationUpdateAsync(
        Guid driverId,
        double latitude,
        double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new HubException("Coordenadas invalidas.");

        var driver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == driverId);

        if (driver == null)
            throw new HubException("Motorista nao encontrado.");

        if (Context.User?.IsInRole("Administrator") != true)
        {
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              Context.User?.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId) || driver.UserId != userId)
                throw new HubException("Motorista nao autorizado para esta localizacao.");
        }

        if (!driver.Active)
            throw new HubException("Motorista inativo.");

        if (driver.ApprovalStatus != DriverApprovalStatus.Approved)
            throw new HubException("Motorista ainda nao aprovado.");

        if (driver.Status != DriverStatus.Online)
            throw new HubException("Motorista precisa estar online para atualizar localizacao.");
    }
}
