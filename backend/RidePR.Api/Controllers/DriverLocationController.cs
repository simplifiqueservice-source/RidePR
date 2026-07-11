using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RidePR.Application.Interfaces;
using RidePR.Application.Services;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/driver-location")]
public class DriverLocationController : ControllerBase
{
    private readonly DriverLocationService _service;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly ApplicationDbContext _context;

    public DriverLocationController(
        DriverLocationService service,
        IRealtimeNotifier realtimeNotifier,
        ApplicationDbContext context)
    {
        _service = service;
        _realtimeNotifier = realtimeNotifier;
        _context = context;
    }

    // ==========================
    // Atualizar localização
    // ==========================
    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost]
    public async Task<IActionResult> Update(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        var validation = await ValidateDriverLocationUpdateAsync(driverId, latitude, longitude);
        if (validation != null)
            return validation;

        await _service.UpdateLocationAsync(
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

        return Ok();
    }

    private async Task<IActionResult?> ValidateDriverLocationUpdateAsync(
        Guid driverId,
        double latitude,
        double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return BadRequest("Coordenadas invalidas.");

        var driver = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == driverId);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        if (!User.IsInRole("Administrator"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var userId) || driver.UserId != userId)
                return Forbid();
        }

        if (!driver.Active)
            return BadRequest("Motorista inativo.");

        if (driver.ApprovalStatus != DriverApprovalStatus.Approved)
            return BadRequest("Motorista ainda nao aprovado.");

        if (driver.Status != DriverStatus.Online)
            return BadRequest("Motorista precisa estar online para atualizar localizacao.");

        return null;
    }

    // ==========================
    // Buscar localização
    // ==========================
    [Authorize(Roles = "Administrator,Driver")]
    [HttpGet("{driverId}")]
    public async Task<IActionResult> Get(Guid driverId)
    {
        var location = await _service.GetDriverLocationAsync(driverId);

        if (location == null)
            return NotFound();

        return Ok(location);
    }

    // ==========================
    // Buscar motoristas próximos
    // ==========================
    [Authorize(Roles = "Administrator,Driver,Passenger")]
    [HttpGet("nearby")]
    public async Task<IActionResult> Nearby(
        double latitude,
        double longitude,
        double radiusKm = 5)
    {
        var drivers = await _service.GetNearbyDriversAsync(
            latitude,
            longitude,
            radiusKm);

        return Ok(drivers.Select(x => new
        {
            x.DriverId,
            Latitude = x.Position.Y,
            Longitude = x.Position.X,
            x.Speed,
            x.Heading,
            x.Online,
            x.UpdatedAt
        }));
    }
}
