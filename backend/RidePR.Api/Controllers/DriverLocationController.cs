using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,Driver")]
[Route("api/driver-location")]
public class DriverLocationController : ControllerBase
{
    private readonly DriverLocationService _service;

    public DriverLocationController(DriverLocationService service)
    {
        _service = service;
    }

    // ==========================
    // Atualizar localização
    // ==========================
    [HttpPost]
    public async Task<IActionResult> Update(
        Guid driverId,
        double latitude,
        double longitude,
        double speed,
        double heading)
    {
        await _service.UpdateLocationAsync(
            driverId,
            latitude,
            longitude,
            speed,
            heading);

        return Ok();
    }

    // ==========================
    // Buscar localização
    // ==========================
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

        return Ok(drivers);
    }
}
