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
[Authorize(Roles = "Driver")]
[Route("api/drivers/me")]
public class DriverMeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly DriverLocationService _locationService;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public DriverMeController(
        ApplicationDbContext context,
        DriverLocationService locationService,
        IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _locationService = locationService;
        _realtimeNotifier = realtimeNotifier;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat()
    {
        var driver = await GetCurrentDriverAsync();

        if (driver == null)
            return NotFound("Motorista nao encontrado para o usuario autenticado.");

        var validation = await ValidateCanBeOnlineAsync(driver.Id);
        if (validation != null)
            return validation;

        driver.Status = DriverStatus.Online;
        driver.UpdatedAt = DateTime.UtcNow;

        var location = await _context.DriverLocations.FirstOrDefaultAsync(x => x.DriverId == driver.Id);
        if (location != null)
        {
            location.Online = true;
            location.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            driverId = driver.Id,
            status = "OnlineAvailable",
            online = true,
            lastHeartbeatAt = DateTime.UtcNow,
            latitude = location?.Position.Y,
            longitude = location?.Position.X
        });
    }

    [HttpPost("location")]
    public async Task<IActionResult> Location(DriverLocationRequest dto)
    {
        var driver = await GetCurrentDriverAsync();

        if (driver == null)
            return NotFound("Motorista nao encontrado para o usuario autenticado.");

        if (dto.Latitude is < -90 or > 90 || dto.Longitude is < -180 or > 180)
            return BadRequest("Coordenadas invalidas.");

        var validation = await ValidateCanBeOnlineAsync(driver.Id);
        if (validation != null)
            return validation;

        driver.Status = DriverStatus.Online;
        driver.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _locationService.UpdateLocationAsync(
            driver.Id,
            dto.Latitude,
            dto.Longitude,
            dto.Speed,
            dto.Heading);

        await _realtimeNotifier.NotifyDriverLocationUpdatedAsync(
            driver.Id,
            dto.Latitude,
            dto.Longitude,
            dto.Speed,
            dto.Heading);

        return Ok(new
        {
            driverId = driver.Id,
            status = "OnlineAvailable",
            online = true,
            lastHeartbeatAt = DateTime.UtcNow,
            latitude = dto.Latitude,
            longitude = dto.Longitude
        });
    }

    private async Task<RidePR.Domain.Entities.Driver?> GetCurrentDriverAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return null;

        return await _context.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private async Task<IActionResult?> ValidateCanBeOnlineAsync(Guid driverId)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(x => x.Id == driverId);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        if (!driver.Active)
            return BadRequest("Motorista inativo.");

        if (driver.ApprovalStatus != DriverApprovalStatus.Approved)
            return BadRequest("Motorista ainda nao aprovado.");

        var hasActiveVehicle = await _context.Vehicles.AnyAsync(x => x.DriverId == driverId && x.Active);
        if (!hasActiveVehicle)
            return BadRequest("Motorista precisa ter veiculo ativo.");

        return null;
    }

    public class DriverLocationRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public double Heading { get; set; }
        public double Speed { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
