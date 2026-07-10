using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/branch-fares")]
[Route("api/admin/fares")]
public class BranchFaresController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BranchFaresController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? branchId)
    {
        var admin = await CurrentUserAsync();
        var query = _context.FareSettings.Include(x => x.Branch).AsQueryable();

        if (admin?.AdminType == AdminType.AdminFilial && admin.BranchId.HasValue)
            query = query.Where(x => x.BranchId == admin.BranchId.Value);
        else if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var rows = await query
            .OrderBy(x => x.Branch == null ? "" : x.Branch.Name)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.BranchId,
                BranchName = x.Branch == null ? "Global" : x.Branch.Name,
                x.Name,
                x.BaseFare,
                x.MinimumFare,
                x.PricePerKm,
                x.PricePerMinute,
                x.CancellationFee,
                x.Active
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(BranchFareDto dto)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial)
        {
            if (!admin.BranchId.HasValue || dto.BranchId != admin.BranchId.Value)
                return Forbid();
        }

        var fare = dto.Id.HasValue
            ? await _context.FareSettings.FirstOrDefaultAsync(x => x.Id == dto.Id.Value)
            : await _context.FareSettings.FirstOrDefaultAsync(x => x.BranchId == dto.BranchId);

        if (fare == null)
        {
            fare = new FareSettings
            {
                Id = Guid.NewGuid(),
                BranchId = dto.BranchId
            };
            await _context.FareSettings.AddAsync(fare);
        }

        fare.Name = string.IsNullOrWhiteSpace(dto.Name) ? "Padrao" : dto.Name.Trim();
        fare.BaseFare = dto.BaseFare;
        fare.MinimumFare = dto.MinimumFare;
        fare.IncludedDistanceKm = 0;
        fare.PricePerKm = dto.PricePerKm;
        fare.PricePerMinute = dto.PricePerMinute;
        fare.CancellationFee = dto.CancellationFee;
        fare.Active = dto.Active;
        fare.DynamicMultiplier = fare.DynamicMultiplier <= 0 ? 1 : fare.DynamicMultiplier;

        await _context.SaveChangesAsync();

        return Ok(fare);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BranchFareDto dto)
    {
        dto.Id = id;
        return await Upsert(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var admin = await CurrentUserAsync();
        var fare = await _context.FareSettings.FirstOrDefaultAsync(x => x.Id == id);

        if (fare == null)
            return NotFound("Tarifa nao encontrada.");

        if (admin?.AdminType == AdminType.AdminFilial &&
            (!admin.BranchId.HasValue || fare.BranchId != admin.BranchId.Value))
            return Forbid();

        _context.FareSettings.Remove(fare);
        await _context.SaveChangesAsync();

        return Ok("Tarifa excluida com sucesso.");
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        return await SetActiveAsync(id, true, "Tarifa ativada.");
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id)
    {
        return await SetActiveAsync(id, false, "Tarifa desativada.");
    }

    private async Task<IActionResult> SetActiveAsync(Guid id, bool active, string message)
    {
        var admin = await CurrentUserAsync();
        var fare = await _context.FareSettings.FirstOrDefaultAsync(x => x.Id == id);

        if (fare == null)
            return NotFound("Tarifa nao encontrada.");

        if (admin?.AdminType == AdminType.AdminFilial &&
            (!admin.BranchId.HasValue || fare.BranchId != admin.BranchId.Value))
            return Forbid();

        fare.Active = active;
        await _context.SaveChangesAsync();

        return Ok(new { Message = message, fare.Active });
    }

    private async Task<User?> CurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var id)
            ? await _context.Users.FirstOrDefaultAsync(x => x.Id == id)
            : null;
    }

    public class BranchFareDto
    {
        public Guid? Id { get; set; }
        public Guid? BranchId { get; set; }
        public string Name { get; set; } = "Padrao";
        public decimal BaseFare { get; set; }
        public decimal MinimumFare { get; set; }
        public decimal PricePerKm { get; set; }
        public decimal PricePerMinute { get; set; }
        public decimal CancellationFee { get; set; }
        public bool Active { get; set; } = true;
    }
}
