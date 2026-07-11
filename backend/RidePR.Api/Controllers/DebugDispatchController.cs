using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/debug")]
public class DebugDispatchController : ControllerBase
{
    private readonly IDispatchNotifier _dispatchNotifier;
    private readonly IWebHostEnvironment _environment;

    public DebugDispatchController(
        IDispatchNotifier dispatchNotifier,
        IWebHostEnvironment environment)
    {
        _dispatchNotifier = dispatchNotifier;
        _environment = environment;
    }

    [HttpPost("drivers/{driverId:guid}/test-offer")]
    public async Task<IActionResult> SendTestOffer(Guid driverId)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var now = DateTime.UtcNow;
        var offer = new DispatchOfferDto
        {
            TripId = Guid.NewGuid(),
            DriverId = driverId,
            OfferedAt = now,
            ExpiresAt = now.AddSeconds(30),
            DistanceKm = 1.4m,
            EtaMinutes = 5,
            Price = 18.90m,
            Origin = "Origem diagnostico RidePR",
            Destination = "Destino diagnostico RidePR"
        };

        await _dispatchNotifier.NotifyOfferAsync(driverId, offer);

        return Ok(new
        {
            Message = "DispatchOfferReceived de diagnostico enviado.",
            offer.TripId,
            offer.DriverId,
            offer.ExpiresAt
        });
    }
}
