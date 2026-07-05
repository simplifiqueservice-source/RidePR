using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RidePR.Application.DTOs;
using RidePR.Application.Services;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _vehicleService;
    private readonly IWebHostEnvironment _environment;

    public VehiclesController(VehicleService vehicleService, IWebHostEnvironment environment)
    {
        _vehicleService = vehicleService;
        _environment = environment;
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] VehicleQueryDto query)
    {
        var result = await _vehicleService.GetPagedAsync(query);

        return Ok(result);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _vehicleService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateVehicleDto dto)
    {
        var result = await _vehicleService.CreateAsync(dto);

        if (!result.Success)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = "Administrator,Driver")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleDto dto)
    {
        var result = await _vehicleService.UpdateAsync(id, dto);

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
        IFormFile? registrationDocument)
    {
        var folder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "vehicles", id.ToString());
        Directory.CreateDirectory(folder);

        var photoUrl = await SaveFileAsync(id, folder, "photo", photo);
        var registrationDocumentUrl = await SaveFileAsync(id, folder, "registration-document", registrationDocument);

        var result = await _vehicleService.UpdateDocumentsAsync(id, photoUrl, registrationDocumentUrl);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _vehicleService.DeactivateAsync(id);

        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Message);
    }

    private static async Task<string?> SaveFileAsync(
        Guid vehicleId,
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

        return $"/uploads/vehicles/{vehicleId}/{fileName}";
    }
}
