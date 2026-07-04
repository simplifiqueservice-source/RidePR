using Microsoft.AspNetCore.Mvc;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Route("api/dispatch")]
public class DispatchController : ControllerBase
{
    private readonly DispatchService _dispatchService;

    public DispatchController(DispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    /// <summary>
    /// Procura motoristas próximos
    /// </summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> NearbyDrivers(
        double latitude,
        double longitude,
        double radiusKm = 5)
    {
        var drivers = await _dispatchService.FindNearbyDriversAsync(
            latitude,
            longitude,
            radiusKm);

        return Ok(drivers);
    }
}