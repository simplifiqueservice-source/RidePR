using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin-panel")]
public class AdminPanelController : ControllerBase
{
    private readonly AdminPanelService _adminPanelService;

    public AdminPanelController(AdminPanelService adminPanelService)
    {
        _adminPanelService = adminPanelService;
    }

    /// <summary>
    /// Retorna o resumo executivo do painel administrativo.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] AdminDashboardQueryDto query)
    {
        var result = await _adminPanelService.GetDashboardAsync(query);

        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna alertas e contadores operacionais para o backoffice.
    /// </summary>
    [HttpGet("operations")]
    public async Task<IActionResult> GetOperations()
    {
        var result = await _adminPanelService.GetOperationsAsync();

        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna a serie diaria de receita e estornos.
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] AdminDashboardQueryDto query)
    {
        var result = await _adminPanelService.GetRevenueAsync(query);

        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna a linha do tempo consolidada de atividades recentes.
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] AdminRecentActivityQueryDto query)
    {
        var result = await _adminPanelService.GetRecentActivityAsync(query);

        return Ok(result.Data);
    }

    /// <summary>
    /// Retorna motoristas com ultima localizacao conhecida para o mapa operacional.
    /// </summary>
    [HttpGet("live-drivers")]
    [HttpGet("/api/admin/live-drivers")]
    public async Task<IActionResult> GetLiveDrivers([FromQuery] AdminLiveDriversQueryDto query)
    {
        var result = await _adminPanelService.GetLiveDriversAsync(query);

        return Ok(result.Data);
    }
}
