using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/passengers")]
public class PassengersController : ControllerBase
{
    private readonly PassengerService _passengerService;

    public PassengersController(PassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    /// <summary>
    /// Lista passageiros com pesquisa e paginacao.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PassengerQueryDto query)
    {
        var result = await _passengerService.GetPagedAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Busca um passageiro pelo identificador do cadastro.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _passengerService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Busca um passageiro pelo identificador do usuario.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var result = await _passengerService.GetByUserIdAsync(userId);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Lista o historico operacional de um passageiro.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _passengerService.GetHistoryAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Cria um cadastro de passageiro vinculado a um usuario Passenger.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePassengerDto dto)
    {
        var result = await _passengerService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Atualiza dados cadastrais do passageiro.
    /// </summary>
    [Authorize(Roles = "Administrator,Passenger")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePassengerDto dto)
    {
        var result = await _passengerService.UpdateAsync(id, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Desativa um passageiro sem remover o registro.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _passengerService.DeactivateAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Message);
    }
}
