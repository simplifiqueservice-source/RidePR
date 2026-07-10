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
[Route("api/branches")]
[Route("api/admin/branches")]
public class BranchesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BranchesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var admin = await CurrentUserAsync();
        var query = _context.Branches.AsQueryable();

        if (admin?.AdminType == AdminType.AdminFilial && admin.BranchId.HasValue)
            query = query.Where(x => x.Id == admin.BranchId.Value);

        var branches = await query.OrderBy(x => x.Name).ToListAsync();
        var branchIds = branches.Select(x => x.Id).ToList();
        var responsibleByBranch = await _context.Users
            .Where(x => x.Role == UserRole.Administrator && x.BranchId.HasValue && branchIds.Contains(x.BranchId.Value))
            .GroupBy(x => x.BranchId!.Value)
            .Select(x => new { BranchId = x.Key, ResponsibleName = x.OrderBy(u => u.Name).Select(u => u.Name).FirstOrDefault() })
            .ToDictionaryAsync(x => x.BranchId, x => x.ResponsibleName ?? "");

        return Ok(branches.Select(x => new
        {
            x.Id,
            x.Name,
            x.City,
            x.State,
            x.Address,
            x.Phone,
            ResponsibleName = responsibleByBranch.GetValueOrDefault(x.Id, "Sem responsavel"),
            x.Active,
            x.CreatedAt,
            x.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(BranchDto dto)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial)
            return Forbid();

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
            Address = dto.Address.Trim(),
            Phone = dto.Phone.Trim(),
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Branches.AddAsync(branch);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = branch.Id }, branch);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BranchDto dto)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial && admin.BranchId != id)
            return Forbid();

        var branch = await _context.Branches.FindAsync(id);

        if (branch == null)
            return NotFound("Filial nao encontrada.");

        branch.Name = dto.Name.Trim();
        branch.City = dto.City.Trim();
        branch.State = dto.State.Trim().ToUpperInvariant();
        branch.Address = dto.Address.Trim();
        branch.Phone = dto.Phone.Trim();
        branch.Active = dto.Active;
        branch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(branch);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial)
            return Forbid();

        var branch = await _context.Branches.FindAsync(id);

        if (branch == null)
            return NotFound("Filial nao encontrada.");

        var inUse = await _context.Users.AnyAsync(x => x.BranchId == id) ||
                    await _context.Drivers.AnyAsync(x => x.BranchId == id) ||
                    await _context.Passengers.AnyAsync(x => x.BranchId == id) ||
                    await _context.Trips.AnyAsync(x => x.BranchId == id);

        if (inUse)
            return Conflict("Filial possui vinculos. Desative a filial antes de remove-la definitivamente.");

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();

        return Ok("Filial excluida com sucesso.");
    }

    private async Task<User?> CurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var id)
            ? await _context.Users.FirstOrDefaultAsync(x => x.Id == id)
            : null;
    }

    public class BranchDto
    {
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public bool Active { get; set; } = true;
    }
}
