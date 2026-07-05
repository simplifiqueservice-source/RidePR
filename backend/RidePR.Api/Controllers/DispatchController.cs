using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dispatch")]
public class DispatchController : ControllerBase
{
    private readonly DispatchService _dispatchService;

    public DispatchController(DispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    /// <summary>
    /// Busca motoristas disponiveis por raio com distancia e ETA ate a origem.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet("nearby")]
    public async Task<IActionResult> NearbyDrivers([FromQuery] DispatchNearbyQueryDto query)
    {
        var result = await _dispatchService.FindCandidatesAsync(query);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Inicia a fila de despacho para uma corrida e envia oferta ao melhor motorista.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("start")]
    public async Task<IActionResult> Start(DispatchRequestDto dto)
    {
        var result = await _dispatchService.StartDispatchAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Consulta o estado da fila de despacho de uma corrida.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet("{tripId:guid}")]
    public async Task<IActionResult> GetState(Guid tripId)
    {
        var result = await _dispatchService.GetStateAsync(tripId);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Aceita a oferta atual de despacho.
    /// </summary>
    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost("{tripId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid tripId, DispatchDriverDecisionDto dto)
    {
        var result = await _dispatchService.AcceptAsync(tripId, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Recusa a oferta atual e reatribui para o proximo motorista da fila.
    /// </summary>
    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost("{tripId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid tripId, DispatchDriverDecisionDto dto)
    {
        var result = await _dispatchService.RejectAsync(tripId, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Forca reatribuicao da corrida para o proximo motorista disponivel.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("{tripId:guid}/reassign")]
    public async Task<IActionResult> Reassign(Guid tripId)
    {
        var result = await _dispatchService.ReassignAsync(tripId);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }
}
