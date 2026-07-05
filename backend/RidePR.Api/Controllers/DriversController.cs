using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/drivers")]
public class DriversController : ControllerBase
{
    private readonly DriverService _driverService;
    private readonly IWebHostEnvironment _environment;

    public DriversController(DriverService driverService, IWebHostEnvironment environment)
    {
        _driverService = driverService;
        _environment = environment;
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DriverQueryDto query)
    {
        var result = await _driverService.GetPagedAsync(query);

        return Ok(result);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _driverService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var result = await _driverService.GetByUserIdAsync(userId);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateDriverDto dto)
    {
        var result = await _driverService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDriverDto dto)
    {
        var result = await _driverService.UpdateAsync(id, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateDriverStatusDto dto)
    {
        var result = await _driverService.UpdateStatusAsync(id, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPatch("{id:guid}/approval")]
    public async Task<IActionResult> SetApproval(Guid id, DriverApprovalDto dto)
    {
        var result = await _driverService.SetApprovalAsync(id, dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost("{id:guid}/documents")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocuments(
        Guid id,
        IFormFile? photo,
        IFormFile? cnhFront,
        IFormFile? cnhBack)
    {
        var folder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "drivers", id.ToString());
        Directory.CreateDirectory(folder);

        var photoUrl = await SaveFileAsync(id, folder, "photo", photo);
        var cnhFrontUrl = await SaveFileAsync(id, folder, "cnh-front", cnhFront);
        var cnhBackUrl = await SaveFileAsync(id, folder, "cnh-back", cnhBack);

        var result = await _driverService.UpdateDocumentsAsync(id, photoUrl, cnhFrontUrl, cnhBackUrl);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _driverService.DeactivateAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Message);
    }

    private static async Task<string?> SaveFileAsync(
        Guid driverId,
        string folder,
        string name,
        IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{name}{extension}";
        var path = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return $"/uploads/drivers/{driverId}/{fileName}";
    }
}
