using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin-users")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminUsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var admin = await CurrentUserAsync();
        var query = _context.Users
            .Include(x => x.Branch)
            .Where(x => x.Role == UserRole.Administrator);

        if (admin?.AdminType == AdminType.AdminFilial && admin.BranchId.HasValue)
            query = query.Where(x => x.BranchId == admin.BranchId.Value || x.Id == admin.Id);

        var rows = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Email,
                AdminType = (x.AdminType ?? AdminType.AdminPrincipal).ToString(),
                x.BranchId,
                BranchName = x.Branch == null ? "" : x.Branch.Name,
                x.Active,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminUserDto dto)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial)
            return Forbid();

        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == dto.Email.ToLower()))
            return BadRequest("E-mail ja cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Administrator,
            AdminType = dto.AdminType,
            BranchId = dto.BranchId,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Name, user.Email, AdminType = user.AdminType.ToString(), user.BranchId, user.Active });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AdminUserDto dto)
    {
        var admin = await CurrentUserAsync();

        if (admin?.AdminType == AdminType.AdminFilial && admin.Id != id)
            return Forbid();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Administrator);

        if (user == null)
            return NotFound("Admin nao encontrado.");

        user.Name = dto.Name.Trim();
        user.Email = dto.Email.Trim();
        user.AdminType = dto.AdminType;
        user.BranchId = dto.BranchId;
        user.Active = dto.Active;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Name, user.Email, AdminType = user.AdminType.ToString(), user.BranchId, user.Active });
    }

    private async Task<User?> CurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var id)
            ? await _context.Users.FirstOrDefaultAsync(x => x.Id == id)
            : null;
    }

    public class AdminUserDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public AdminType AdminType { get; set; } = AdminType.AdminFilial;
        public Guid? BranchId { get; set; }
        public bool Active { get; set; } = true;
    }
}
