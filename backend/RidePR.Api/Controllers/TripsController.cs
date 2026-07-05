using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly TripService _service;

    public TripsController(TripService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista corridas cadastradas.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger,Driver")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    /// <summary>
    /// Consulta uma corrida pelo identificador.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger,Driver")]
    [HttpGet("{tripId:guid}")]
    public async Task<IActionResult> GetById(Guid tripId)
    {
        var trip = await _service.GetByIdAsync(tripId);

        if (trip == null)
            return NotFound("Corrida nao encontrada.");

        return Ok(trip);
    }

    /// <summary>
    /// Passageiro solicita uma nova corrida.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTripDto dto)
    {
        var trip = await _service.CreateAsync(dto);
        return Ok(trip);
    }

    /// <summary>
    /// Motorista inicia uma corrida aceita.
    /// </summary>
    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost("{tripId:guid}/start")]
    public async Task<IActionResult> Start(Guid tripId, StartTripDto dto)
    {
        var trip = await _service.StartTripAsync(tripId, dto.DriverId);

        if (trip == null)
            return BadRequest("Corrida nao encontrada, nao aceita ou motorista invalido.");

        return Ok(trip);
    }

    /// <summary>
    /// Motorista finaliza uma corrida em andamento e recalcula o valor final.
    /// </summary>
    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost("{tripId:guid}/finish")]
    public async Task<IActionResult> Finish(Guid tripId, FinishTripDto dto)
    {
        var trip = await _service.FinishTripAsync(
            tripId,
            dto.DriverId,
            dto.ActualDistanceKm,
            dto.ActualDurationMinutes);

        if (trip == null)
            return BadRequest("Corrida nao encontrada, nao iniciada ou motorista invalido.");

        return Ok(trip);
    }
}
