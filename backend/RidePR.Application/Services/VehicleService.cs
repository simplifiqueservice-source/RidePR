using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Shared.Pagination;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class VehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverRepository _driverRepository;

    public VehicleService(IVehicleRepository vehicleRepository, IDriverRepository driverRepository)
    {
        _vehicleRepository = vehicleRepository;
        _driverRepository = driverRepository;
    }

    public async Task<PagedResult<VehicleResponseDto>> GetPagedAsync(VehicleQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var vehicles = await _vehicleRepository.GetPagedAsync(
            query.Search,
            query.DriverId,
            query.Active,
            page,
            pageSize);

        var total = await _vehicleRepository.CountAsync(
            query.Search,
            query.DriverId,
            query.Active);

        return new PagedResult<VehicleResponseDto>
        {
            Items = vehicles.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Result<VehicleResponseDto>> GetByIdAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);

        if (vehicle == null)
            return Result<VehicleResponseDto>.Fail("Veiculo nao encontrado.");

        return Result<VehicleResponseDto>.Ok(ToResponse(vehicle));
    }

    public async Task<Result<VehicleResponseDto>> CreateAsync(CreateVehicleDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(dto.DriverId);

        if (driver == null)
            return Result<VehicleResponseDto>.Fail("Motorista nao encontrado.");

        if (!driver.Active)
            return Result<VehicleResponseDto>.Fail("Motorista inativo.");

        var normalizedPlate = NormalizePlate(dto.Plate);

        if (await _vehicleRepository.PlateExistsAsync(normalizedPlate))
            return Result<VehicleResponseDto>.Fail("Placa ja cadastrada.");

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            DriverId = dto.DriverId,
            Driver = driver,
            Plate = normalizedPlate,
            Model = dto.Model.Trim(),
            Brand = dto.Brand.Trim(),
            Color = dto.Color.Trim(),
            Year = dto.Year,
            Renavam = dto.Renavam.Trim(),
            Chassis = dto.Chassis.Trim().ToUpperInvariant(),
            CompanyId = dto.CompanyId,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _vehicleRepository.AddAsync(vehicle);
        await _vehicleRepository.SaveChangesAsync();

        return Result<VehicleResponseDto>.Ok(ToResponse(vehicle));
    }

    public async Task<Result<VehicleResponseDto>> UpdateAsync(Guid id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);

        if (vehicle == null)
            return Result<VehicleResponseDto>.Fail("Veiculo nao encontrado.");

        var normalizedPlate = NormalizePlate(dto.Plate);

        if (await _vehicleRepository.PlateExistsAsync(normalizedPlate, id))
            return Result<VehicleResponseDto>.Fail("Placa ja cadastrada.");

        vehicle.Plate = normalizedPlate;
        vehicle.Model = dto.Model.Trim();
        vehicle.Brand = dto.Brand.Trim();
        vehicle.Color = dto.Color.Trim();
        vehicle.Year = dto.Year;
        vehicle.Renavam = dto.Renavam.Trim();
        vehicle.Chassis = dto.Chassis.Trim().ToUpperInvariant();
        vehicle.CompanyId = dto.CompanyId;
        vehicle.Active = dto.Active;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);
        await _vehicleRepository.SaveChangesAsync();

        return Result<VehicleResponseDto>.Ok(ToResponse(vehicle));
    }

    public async Task<Result<VehicleResponseDto>> UpdateDocumentsAsync(
        Guid id,
        string? photoUrl,
        string? registrationDocumentUrl)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);

        if (vehicle == null)
            return Result<VehicleResponseDto>.Fail("Veiculo nao encontrado.");

        if (!string.IsNullOrWhiteSpace(photoUrl))
            vehicle.PhotoUrl = photoUrl;

        if (!string.IsNullOrWhiteSpace(registrationDocumentUrl))
            vehicle.RegistrationDocumentUrl = registrationDocumentUrl;

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);
        await _vehicleRepository.SaveChangesAsync();

        return Result<VehicleResponseDto>.Ok(ToResponse(vehicle));
    }

    public async Task<Result> DeactivateAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);

        if (vehicle == null)
            return Result.Fail("Veiculo nao encontrado.");

        vehicle.Active = false;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle);
        await _vehicleRepository.SaveChangesAsync();

        return Result.Ok("Veiculo desativado com sucesso.");
    }

    private static VehicleResponseDto ToResponse(Vehicle vehicle)
    {
        return new VehicleResponseDto
        {
            Id = vehicle.Id,
            DriverId = vehicle.DriverId,
            DriverName = vehicle.Driver?.User?.Name ?? "",
            Plate = vehicle.Plate,
            Model = vehicle.Model,
            Brand = vehicle.Brand,
            Color = vehicle.Color,
            Year = vehicle.Year,
            Renavam = vehicle.Renavam,
            Chassis = vehicle.Chassis,
            CompanyId = vehicle.CompanyId,
            Active = vehicle.Active,
            PhotoUrl = vehicle.PhotoUrl,
            RegistrationDocumentUrl = vehicle.RegistrationDocumentUrl,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
    }

    private static string NormalizePlate(string plate)
    {
        return plate.Trim().Replace("-", "").ToUpperInvariant();
    }
}
