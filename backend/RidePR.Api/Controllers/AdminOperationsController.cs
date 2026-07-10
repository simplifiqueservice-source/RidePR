using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin")]
public class AdminOperationsController : ControllerBase
{
    private const string CannotDeleteWithHistory = "Nao e possivel excluir. Registro possui historico. Use Bloquear/Desativar.";
    private readonly ApplicationDbContext _context;

    public AdminOperationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var trips = await _context.Trips
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt >= today ||
                x.Status == TripStatus.Requested ||
                x.Status == TripStatus.Accepted ||
                x.Status == TripStatus.InProgress)
            .ToListAsync();

        return Ok(new
        {
            GeneratedAt = DateTime.UtcNow,
            Trips = new
            {
                Waiting = trips.Count(x => x.Status == TripStatus.Requested),
                Active = trips.Count(x => x.Status == TripStatus.Accepted || x.Status == TripStatus.InProgress),
                FinishedToday = trips.Count(x => x.Status == TripStatus.Finished && x.CreatedAt >= today && x.CreatedAt < tomorrow),
                RevenueToday = trips
                    .Where(x => x.CreatedAt >= today && x.CreatedAt < tomorrow && x.Status != TripStatus.Cancelled)
                    .Sum(x => x.Price)
            },
            Drivers = new
            {
                Total = await _context.Drivers.CountAsync(),
                Online = await _context.Drivers.CountAsync(x => x.Status == DriverStatus.Online),
                PendingApproval = await _context.Drivers.CountAsync(x => x.ApprovalStatus == DriverApprovalStatus.Pending)
            },
            Passengers = new
            {
                Total = await _context.Passengers.CountAsync(),
                Active = await _context.Passengers.CountAsync(x => x.Active)
            },
            Branches = new
            {
                Total = await _context.Branches.CountAsync(),
                Active = await _context.Branches.CountAsync(x => x.Active)
            }
        });
    }

    [HttpGet("trips")]
    public async Task<IActionResult> GetTrips(
        [FromQuery] TripStatus? status,
        [FromQuery] Guid? branchId,
        [FromQuery] string? view = "operational")
    {
        var query = _context.Trips.Include(x => x.Branch).AsNoTracking();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        if (string.Equals(view, "today", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(x => x.CreatedAt >= today);
        }
        else if (!status.HasValue && !string.Equals(view, "all", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(x =>
                x.CreatedAt >= today ||
                x.Status == TripStatus.Requested ||
                x.Status == TripStatus.Accepted ||
                x.Status == TripStatus.InProgress);
        }

        var trips = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();

        return Ok(await BuildTripDtosAsync(trips));
    }

    [HttpGet("trips/{id:guid}")]
    public async Task<IActionResult> GetTrip(Guid id)
    {
        var trip = await _context.Trips
            .Include(x => x.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (trip == null)
            return NotFound("Corrida nao encontrada.");

        return Ok((await BuildTripDtosAsync(new[] { trip })).First());
    }

    [HttpPost("trips/{id:guid}/cancel")]
    [HttpPost("/api/trips/{id:guid}/cancel")]
    public async Task<IActionResult> CancelTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);

        if (trip == null)
            return NotFound("Corrida nao encontrada.");

        if (trip.Status is TripStatus.Finished or TripStatus.Cancelled)
            return BadRequest("Corrida ja finalizada ou cancelada.");

        trip.Status = TripStatus.Cancelled;
        await SetDriverOnlineIfNeededAsync(trip.DriverId);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Corrida cancelada com sucesso.", Status = TripStatusLabel(trip.Status) });
    }

    [HttpPost("trips/{id:guid}/finish")]
    [HttpPost("trips/{id:guid}/finish-admin")]
    [HttpPost("/api/trips/{id:guid}/finish-admin")]
    public async Task<IActionResult> FinishTripByAdmin(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);

        if (trip == null)
            return NotFound("Corrida nao encontrada.");

        if (trip.Status == TripStatus.Cancelled)
            return BadRequest("Corrida cancelada nao pode ser finalizada.");

        if (trip.Status == TripStatus.Finished)
            return Ok(new { Message = "Corrida ja estava finalizada.", Status = TripStatusLabel(trip.Status) });

        trip.Status = TripStatus.Finished;
        trip.ActualDistanceKm = trip.ActualDistanceKm > 0 ? trip.ActualDistanceKm : trip.EstimatedDistanceKm;
        await SetDriverOnlineIfNeededAsync(trip.DriverId);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Corrida finalizada manualmente.", Status = TripStatusLabel(trip.Status) });
    }

    [HttpPost("trips/{id:guid}/redispatch")]
    public async Task<IActionResult> RedispatchTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);

        if (trip == null)
            return NotFound("Corrida nao encontrada.");

        if (trip.Status is TripStatus.Finished or TripStatus.Cancelled)
            return BadRequest("Corrida finalizada ou cancelada nao pode ser reenviada.");

        await SetDriverOnlineIfNeededAsync(trip.DriverId);
        trip.DriverId = null;
        trip.Status = TripStatus.Requested;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Corrida reenviada para busca de motorista.", Status = TripStatusLabel(trip.Status) });
    }

    [HttpPost("trips/cancel-old-pending")]
    public async Task<IActionResult> CancelOldPendingTrips()
    {
        var today = DateTime.UtcNow.Date;
        var trips = await _context.Trips
            .Where(x => x.CreatedAt < today && x.Status == TripStatus.Requested)
            .ToListAsync();

        foreach (var trip in trips)
        {
            trip.Status = TripStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Corridas pendentes antigas canceladas.", Count = trips.Count });
    }

    [HttpGet("passengers")]
    public async Task<IActionResult> GetPassengers([FromQuery] bool? active, [FromQuery] Guid? branchId)
    {
        var query = _context.Passengers
            .Include(x => x.User)
            .Include(x => x.Branch)
            .AsNoTracking();

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var passengers = await query.OrderBy(x => x.User.Name).Take(200).ToListAsync();
        var passengerIds = passengers.Select(x => x.Id).ToList();
        var tripCounts = await _context.Trips
            .Where(x => passengerIds.Contains(x.PassengerId))
            .GroupBy(x => x.PassengerId)
            .Select(x => new { PassengerId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.PassengerId, x => x.Count);

        return Ok(passengers.Select(x => new
        {
            x.Id,
            x.UserId,
            Name = x.User.Name,
            x.User.Email,
            x.Phone,
            x.Cpf,
            x.BirthDate,
            x.EmergencyPhone,
            x.City,
            x.State,
            x.Address,
            x.ZipCode,
            x.BranchId,
            BranchName = x.Branch?.Name ?? x.User.Branch?.Name ?? "Sem filial",
            x.Active,
            StatusLabel = x.Active ? "Aprovado" : "Bloqueado",
            TripsCount = tripCounts.GetValueOrDefault(x.Id)
        }));
    }

    [HttpPost("passengers")]
    public async Task<IActionResult> CreatePassenger(AdminPassengerDto dto)
    {
        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == dto.Email.Trim().ToLower()))
            return BadRequest("E-mail ja cadastrado.");

        if (await _context.Passengers.AnyAsync(x => x.Cpf == dto.Cpf.Trim()))
            return BadRequest("CPF ja cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(dto.Password) ? "RidePR123!" : dto.Password),
            Role = UserRole.Passenger,
            BranchId = dto.BranchId,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        var passenger = new Passenger
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            BranchId = dto.BranchId,
            Cpf = dto.Cpf.Trim(),
            BirthDate = UtcDate(dto.BirthDate),
            Phone = dto.Phone.Trim(),
            EmergencyPhone = dto.EmergencyPhone.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
            ZipCode = dto.ZipCode.Trim(),
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.Passengers.AddAsync(passenger);
        await _context.SaveChangesAsync();

        return Ok(new { passenger.Id, user.Name, user.Email, passenger.Phone, passenger.Cpf, passenger.Active });
    }

    [HttpPut("passengers/{id:guid}")]
    public async Task<IActionResult> UpdatePassenger(Guid id, AdminPassengerDto dto)
    {
        var passenger = await _context.Passengers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (passenger == null)
            return NotFound("Passageiro nao encontrado.");

        passenger.User.Name = dto.Name.Trim();
        passenger.User.Email = dto.Email.Trim();
        passenger.User.BranchId = dto.BranchId;
        passenger.User.Active = dto.Active;
        passenger.BranchId = dto.BranchId;
        passenger.Cpf = dto.Cpf.Trim();
        passenger.BirthDate = UtcDate(dto.BirthDate);
        passenger.Phone = dto.Phone.Trim();
        passenger.EmergencyPhone = dto.EmergencyPhone.Trim();
        passenger.Address = dto.Address.Trim();
        passenger.City = dto.City.Trim();
        passenger.State = dto.State.Trim().ToUpperInvariant();
        passenger.ZipCode = dto.ZipCode.Trim();
        passenger.Active = dto.Active;
        passenger.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            passenger.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();

        return Ok(new { passenger.Id, passenger.User.Name, passenger.User.Email, passenger.Phone, passenger.Cpf, passenger.Active });
    }

    [HttpPost("passengers/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePassenger(Guid id)
    {
        var passenger = await _context.Passengers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (passenger == null)
            return NotFound("Passageiro nao encontrado.");

        passenger.Active = true;
        passenger.User.Active = true;
        passenger.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Passageiro aprovado.", Status = "Aprovado" });
    }

    [HttpPost("passengers/{id:guid}/block")]
    public async Task<IActionResult> BlockPassenger(Guid id)
    {
        var passenger = await _context.Passengers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (passenger == null)
            return NotFound("Passageiro nao encontrado.");

        passenger.Active = false;
        passenger.User.Active = false;
        passenger.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Passageiro bloqueado.", Status = "Bloqueado" });
    }

    [HttpPost("passengers/{id:guid}/disable")]
    public async Task<IActionResult> DisablePassenger(Guid id)
    {
        return await BlockPassenger(id);
    }

    [HttpDelete("passengers/{id:guid}")]
    public async Task<IActionResult> DeletePassenger(Guid id)
    {
        var passenger = await _context.Passengers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (passenger == null)
            return NotFound("Passageiro nao encontrado.");

        if (await _context.Trips.AnyAsync(x => x.PassengerId == id))
            return Conflict(CannotDeleteWithHistory);

        _context.Passengers.Remove(passenger);
        _context.Users.Remove(passenger.User);
        await _context.SaveChangesAsync();

        return Ok("Passageiro excluido com sucesso.");
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers([FromQuery] bool? active, [FromQuery] DriverApprovalStatus? approvalStatus, [FromQuery] Guid? branchId)
    {
        var query = _context.Drivers
            .Include(x => x.User)
            .Include(x => x.Branch)
            .Include(x => x.Vehicles)
            .AsNoTracking();

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        if (approvalStatus.HasValue)
            query = query.Where(x => x.ApprovalStatus == approvalStatus.Value);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var drivers = await query.OrderBy(x => x.User.Name).Take(200).ToListAsync();
        var driverIds = drivers.Select(x => x.Id).ToList();
        var tripCounts = await _context.Trips
            .Where(x => x.DriverId.HasValue && driverIds.Contains(x.DriverId.Value))
            .GroupBy(x => x.DriverId!.Value)
            .Select(x => new { DriverId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.DriverId, x => x.Count);

        return Ok(drivers.Select(x =>
        {
            var vehicle = x.Vehicles.FirstOrDefault(v => v.Active) ?? x.Vehicles.FirstOrDefault();

            return new
            {
                x.Id,
                x.UserId,
                Name = x.User.Name,
                x.User.Email,
                x.Phone,
                x.Cpf,
                x.Rg,
                x.BirthDate,
                x.EmergencyPhone,
                x.Address,
                Cnh = x.CnhNumber,
                x.CnhCategory,
                x.CnhExpiration,
                Vehicle = vehicle == null ? "Sem veiculo" : $"{vehicle.Brand} {vehicle.Model} - {vehicle.Plate}",
                Plate = vehicle?.Plate ?? "",
                x.City,
                x.State,
                x.BranchId,
                BranchName = x.Branch?.Name ?? x.User.Branch?.Name ?? "Sem filial",
                Status = x.Status.ToString(),
                StatusLabel = DriverStatusLabel(x.Status),
                ApprovalStatus = x.ApprovalStatus.ToString(),
                ApprovalStatusLabel = ApprovalStatusLabel(x.ApprovalStatus),
                x.Active,
                ActiveLabel = x.Active ? "Ativo" : "Bloqueado",
                TripsCount = tripCounts.GetValueOrDefault(x.Id)
            };
        }));
    }

    [HttpPost("drivers")]
    public async Task<IActionResult> CreateDriver(AdminDriverDto dto)
    {
        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == dto.Email.Trim().ToLower()))
            return BadRequest("E-mail ja cadastrado.");

        if (await _context.Drivers.AnyAsync(x => x.Cpf == dto.Cpf.Trim() || x.CnhNumber == dto.CnhNumber.Trim()))
            return BadRequest("CPF ou CNH ja cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(dto.Password) ? "RidePR123!" : dto.Password),
            Role = UserRole.Driver,
            BranchId = dto.BranchId,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            BranchId = dto.BranchId,
            Cpf = dto.Cpf.Trim(),
            Rg = dto.Rg.Trim(),
            BirthDate = UtcDate(dto.BirthDate),
            Phone = dto.Phone.Trim(),
            EmergencyPhone = dto.EmergencyPhone.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
            ZipCode = dto.ZipCode.Trim(),
            CnhNumber = dto.CnhNumber.Trim(),
            CnhCategory = dto.CnhCategory.Trim(),
            CnhExpiration = UtcDate(dto.CnhExpiration),
            Status = dto.Active ? DriverStatus.Online : DriverStatus.Offline,
            ApprovalStatus = dto.Approved ? DriverApprovalStatus.Approved : DriverApprovalStatus.Pending,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.Drivers.AddAsync(driver);
        await _context.SaveChangesAsync();

        return Ok(new { driver.Id, user.Name, user.Email, driver.Phone, driver.Cpf, driver.Active });
    }

    [HttpPut("drivers/{id:guid}")]
    public async Task<IActionResult> UpdateDriver(Guid id, AdminDriverDto dto)
    {
        var driver = await _context.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        driver.User.Name = dto.Name.Trim();
        driver.User.Email = dto.Email.Trim();
        driver.User.BranchId = dto.BranchId;
        driver.User.Active = dto.Active;
        driver.BranchId = dto.BranchId;
        driver.Cpf = dto.Cpf.Trim();
        driver.Rg = dto.Rg.Trim();
        driver.BirthDate = UtcDate(dto.BirthDate);
        driver.Phone = dto.Phone.Trim();
        driver.EmergencyPhone = dto.EmergencyPhone.Trim();
        driver.Address = dto.Address.Trim();
        driver.City = dto.City.Trim();
        driver.State = dto.State.Trim().ToUpperInvariant();
        driver.ZipCode = dto.ZipCode.Trim();
        driver.CnhNumber = dto.CnhNumber.Trim();
        driver.CnhCategory = dto.CnhCategory.Trim();
        driver.CnhExpiration = UtcDate(dto.CnhExpiration);
        driver.Active = dto.Active;
        driver.ApprovalStatus = dto.Approved ? DriverApprovalStatus.Approved : driver.ApprovalStatus;
        driver.Status = dto.Active ? driver.Status : DriverStatus.Offline;
        driver.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            driver.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();

        return Ok(new { driver.Id, driver.User.Name, driver.User.Email, driver.Phone, driver.Cpf, driver.Active });
    }

    [HttpPost("drivers/{id:guid}/approve")]
    public async Task<IActionResult> ApproveDriver(Guid id)
    {
        var driver = await _context.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        driver.ApprovalStatus = DriverApprovalStatus.Approved;
        driver.Active = true;
        driver.User.Active = true;
        driver.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Motorista aprovado.", Status = "Aprovado" });
    }

    [HttpPost("drivers/{id:guid}/block")]
    public async Task<IActionResult> BlockDriver(Guid id)
    {
        var driver = await _context.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        driver.Active = false;
        driver.User.Active = false;
        driver.Status = DriverStatus.Offline;
        driver.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Motorista bloqueado.", Status = "Bloqueado" });
    }

    [HttpPost("drivers/{id:guid}/disable")]
    public async Task<IActionResult> DisableDriver(Guid id)
    {
        return await BlockDriver(id);
    }

    [HttpDelete("drivers/{id:guid}")]
    public async Task<IActionResult> DeleteDriver(Guid id)
    {
        var driver = await _context.Drivers.Include(x => x.User).Include(x => x.Vehicles).FirstOrDefaultAsync(x => x.Id == id);

        if (driver == null)
            return NotFound("Motorista nao encontrado.");

        if (await _context.Trips.AnyAsync(x => x.DriverId == id))
            return Conflict(CannotDeleteWithHistory);

        _context.Vehicles.RemoveRange(driver.Vehicles);
        _context.Drivers.Remove(driver);
        _context.Users.Remove(driver.User);
        await _context.SaveChangesAsync();

        return Ok("Motorista excluido com sucesso.");
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles([FromQuery] bool? active, [FromQuery] Guid? branchId)
    {
        var query = _context.Vehicles
            .Include(x => x.Driver)
            .ThenInclude(x => x.User)
            .Include(x => x.Driver)
            .ThenInclude(x => x.Branch)
            .AsNoTracking();

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        if (branchId.HasValue)
            query = query.Where(x => x.Driver.BranchId == branchId.Value);

        var vehicles = await query.OrderBy(x => x.Plate).Take(200).ToListAsync();
        var driverIds = vehicles.Select(x => x.DriverId).Distinct().ToList();
        var tripCounts = await _context.Trips
            .Where(x => x.DriverId.HasValue && driverIds.Contains(x.DriverId.Value))
            .GroupBy(x => x.DriverId!.Value)
            .Select(x => new { DriverId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.DriverId, x => x.Count);

        return Ok(vehicles.Select(x => new
        {
            x.Id,
            x.DriverId,
            DriverName = x.Driver.User.Name,
            x.Brand,
            x.Model,
            x.Color,
            x.Plate,
            x.Year,
            BranchId = x.Driver.BranchId,
            x.Renavam,
            x.Chassis,
            BranchName = x.Driver.Branch?.Name ?? x.Driver.User.Branch?.Name ?? "Sem filial",
            x.Active,
            StatusLabel = x.Active ? "Ativo" : "Inativo",
            TripsCount = tripCounts.GetValueOrDefault(x.DriverId)
        }));
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle(AdminVehicleDto dto)
    {
        if (await _context.Vehicles.AnyAsync(x => x.Plate.ToLower() == dto.Plate.Trim().ToLower()))
            return BadRequest("Placa ja cadastrada.");

        if (!await _context.Drivers.AnyAsync(x => x.Id == dto.DriverId))
            return BadRequest("Motorista nao encontrado.");

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            DriverId = dto.DriverId,
            Plate = dto.Plate.Trim().ToUpperInvariant(),
            Brand = dto.Brand.Trim(),
            Model = dto.Model.Trim(),
            Color = dto.Color.Trim(),
            Year = dto.Year,
            Renavam = dto.Renavam.Trim(),
            Chassis = dto.Chassis.Trim(),
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();

        return Ok(vehicle);
    }

    [HttpPut("vehicles/{id:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid id, AdminVehicleDto dto)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == id);

        if (vehicle == null)
            return NotFound("Veiculo nao encontrado.");

        vehicle.DriverId = dto.DriverId;
        vehicle.Plate = dto.Plate.Trim().ToUpperInvariant();
        vehicle.Brand = dto.Brand.Trim();
        vehicle.Model = dto.Model.Trim();
        vehicle.Color = dto.Color.Trim();
        vehicle.Year = dto.Year;
        vehicle.Renavam = dto.Renavam.Trim();
        vehicle.Chassis = dto.Chassis.Trim();
        vehicle.Active = dto.Active;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(vehicle);
    }

    [HttpPost("vehicles/{id:guid}/approve")]
    public async Task<IActionResult> ApproveVehicle(Guid id)
    {
        return await SetVehicleActiveAsync(id, true, "Veiculo aprovado.", "Ativo");
    }

    [HttpPost("vehicles/{id:guid}/disable")]
    public async Task<IActionResult> DisableVehicle(Guid id)
    {
        return await SetVehicleActiveAsync(id, false, "Veiculo desativado.", "Inativo");
    }

    [HttpDelete("vehicles/{id:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == id);

        if (vehicle == null)
            return NotFound("Veiculo nao encontrado.");

        if (await _context.Trips.AnyAsync(x => x.DriverId == vehicle.DriverId))
            return Conflict(CannotDeleteWithHistory);

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return Ok("Veiculo excluido com sucesso.");
    }

    private async Task<List<object>> BuildTripDtosAsync(IEnumerable<Trip> tripsSource)
    {
        var trips = tripsSource.ToList();
        var passengerIds = trips.Select(x => x.PassengerId).Distinct().ToList();
        var driverIds = trips.Where(x => x.DriverId.HasValue).Select(x => x.DriverId!.Value).Distinct().ToList();

        var passengers = await _context.Passengers
            .Include(x => x.User)
            .AsNoTracking()
            .Where(x => passengerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        var drivers = await _context.Drivers
            .Include(x => x.User)
            .Include(x => x.Vehicles)
            .AsNoTracking()
            .Where(x => driverIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        return trips.Select(trip =>
        {
            passengers.TryGetValue(trip.PassengerId, out var passenger);
            var driver = trip.DriverId.HasValue && drivers.TryGetValue(trip.DriverId.Value, out var foundDriver)
                ? foundDriver
                : null;
            var vehicle = driver?.Vehicles.FirstOrDefault(x => x.Active) ?? driver?.Vehicles.FirstOrDefault();

            return (object)new
            {
                trip.Id,
                ShortCode = ShortCode(trip.Id),
                trip.PassengerId,
                PassengerName = passenger?.User?.Name ?? "Passageiro nao informado",
                PassengerPhone = passenger?.Phone ?? "",
                PassengerCity = passenger?.City ?? "",
                trip.DriverId,
                DriverName = driver?.User?.Name ?? "Aguardando motorista",
                DriverPhone = driver?.Phone ?? "",
                Vehicle = vehicle == null ? "Sem veiculo" : $"{vehicle.Brand} {vehicle.Model} - {vehicle.Plate}",
                VehicleModel = vehicle == null ? "" : $"{vehicle.Brand} {vehicle.Model}",
                VehiclePlate = vehicle?.Plate ?? "",
                BranchId = trip.BranchId,
                BranchName = trip.Branch?.Name ?? "Sem filial",
                Origin = trip.Origin,
                OriginAddress = trip.Origin,
                OriginShort = ShortAddress(trip.Origin),
                Destination = trip.Destination,
                DestinationAddress = trip.Destination,
                DestinationShort = ShortAddress(trip.Destination),
                Status = trip.Status.ToString(),
                StatusLabel = TripStatusLabel(trip.Status),
                FareAmount = trip.Price,
                Price = trip.Price,
                trip.EstimatedDistanceKm,
                trip.EstimatedDurationMinutes,
                trip.ActualDistanceKm,
                trip.OriginLatitude,
                trip.OriginLongitude,
                trip.DestinationLatitude,
                trip.DestinationLongitude,
                trip.CreatedAt,
                AcceptedAt = (DateTime?)null,
                StartedAt = (DateTime?)null,
                FinishedAt = trip.Status == TripStatus.Finished ? trip.CreatedAt : (DateTime?)null
            };
        }).ToList();
    }

    private async Task<IActionResult> SetVehicleActiveAsync(Guid id, bool active, string message, string status)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == id);

        if (vehicle == null)
            return NotFound("Veiculo nao encontrado.");

        vehicle.Active = active;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = message, Status = status });
    }

    private async Task SetDriverOnlineIfNeededAsync(Guid? driverId)
    {
        if (!driverId.HasValue)
            return;

        var driver = await _context.Drivers.FindAsync(driverId.Value);

        if (driver == null)
            return;

        driver.Status = DriverStatus.Online;
        driver.UpdatedAt = DateTime.UtcNow;
    }

    private static string ShortCode(Guid id)
    {
        return id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string ShortAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "-";

        var parts = address
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToList();

        var shortAddress = parts.Count == 0 ? address.Trim() : string.Join(", ", parts);
        return shortAddress.Length > 58 ? $"{shortAddress[..55]}..." : shortAddress;
    }

    private static string TripStatusLabel(TripStatus status)
    {
        return status switch
        {
            TripStatus.Requested => "Aguardando motorista",
            TripStatus.Accepted => "Aceita",
            TripStatus.InProgress => "Em andamento",
            TripStatus.Finished => "Finalizada",
            TripStatus.Cancelled => "Cancelada",
            _ => "Desconhecido"
        };
    }

    private static string DriverStatusLabel(DriverStatus status)
    {
        return status switch
        {
            DriverStatus.Offline => "Offline",
            DriverStatus.Online => "Online",
            DriverStatus.Busy => "Em corrida",
            DriverStatus.Paused => "Pausado",
            _ => "Desconhecido"
        };
    }

    private static string ApprovalStatusLabel(DriverApprovalStatus status)
    {
        return status switch
        {
            DriverApprovalStatus.Pending => "Pendente",
            DriverApprovalStatus.Approved => "Aprovado",
            DriverApprovalStatus.Rejected => "Recusado",
            _ => "Desconhecido"
        };
    }

    private static DateTime UtcDate(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public class AdminPassengerDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public Guid? BranchId { get; set; }
        public string Cpf { get; set; } = "";
        public DateTime BirthDate { get; set; } = DateTime.UtcNow.AddYears(-18);
        public string Phone { get; set; } = "";
        public string EmergencyPhone { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public bool Active { get; set; } = true;
    }

    public class AdminDriverDto : AdminPassengerDto
    {
        public string Rg { get; set; } = "";
        public string CnhNumber { get; set; } = "";
        public string CnhCategory { get; set; } = "";
        public DateTime CnhExpiration { get; set; } = DateTime.UtcNow.AddYears(1);
        public bool Approved { get; set; } = true;
    }

    public class AdminVehicleDto
    {
        public Guid DriverId { get; set; }
        public string Plate { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string Color { get; set; } = "";
        public int Year { get; set; } = DateTime.UtcNow.Year;
        public string Renavam { get; set; } = "";
        public string Chassis { get; set; } = "";
        public bool Active { get; set; } = true;
    }
}
