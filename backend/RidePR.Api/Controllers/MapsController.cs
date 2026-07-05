using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/maps")]
public class MapsController : ControllerBase
{
    private readonly RouteService _routeService;

    public MapsController(RouteService routeService)
    {
        _routeService = routeService;
    }

    /// <summary>
    /// Calcula uma rota entre origem e destino usando o provider configurado ou informado.
    /// </summary>
    [HttpPost("route")]
    public async Task<IActionResult> Route(MapRouteRequestDto dto)
    {
        var result = await _routeService.GetRouteAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Calcula matriz de distancia e duracao entre multiplas origens e destinos.
    /// </summary>
    [HttpPost("distance-matrix")]
    public async Task<IActionResult> DistanceMatrix(DistanceMatrixRequestDto dto)
    {
        var result = await _routeService.GetDistanceMatrixAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Calcula ETA entre origem e destino.
    /// </summary>
    [HttpPost("eta")]
    public async Task<IActionResult> Eta(EtaRequestDto dto)
    {
        var result = await _routeService.GetEtaAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Converte endereco textual em coordenadas geograficas.
    /// </summary>
    [HttpPost("geocode")]
    public async Task<IActionResult> Geocode(GeocodingRequestDto dto)
    {
        var result = await _routeService.GeocodeAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Converte coordenadas geograficas em endereco textual.
    /// </summary>
    [HttpPost("reverse-geocode")]
    public async Task<IActionResult> ReverseGeocode(ReverseGeocodingRequestDto dto)
    {
        var result = await _routeService.ReverseGeocodeAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }
}
