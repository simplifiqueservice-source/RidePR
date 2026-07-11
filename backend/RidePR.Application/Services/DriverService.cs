using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Shared.Pagination;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class DriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDriverLocationRepository _locationRepository;

    public DriverService(
        IDriverRepository driverRepository,
        IUserRepository userRepository,
        IDriverLocationRepository locationRepository)
    {
        _driverRepository = driverRepository;
        _userRepository = userRepository;
        _locationRepository = locationRepository;
    }

    public async Task<PagedResult<DriverResponseDto>> GetPagedAsync(DriverQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var drivers = await _driverRepository.GetPagedAsync(
            query.Search,
            query.Status,
            query.ApprovalStatus,
            query.Active,
            page,
            pageSize);

        var total = await _driverRepository.CountAsync(
            query.Search,
            query.Status,
            query.ApprovalStatus,
            query.Active);

        return new PagedResult<DriverResponseDto>
        {
            Items = drivers.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Result<DriverResponseDto>> GetByIdAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> GetByUserIdAsync(Guid userId)
    {
        var driver = await _driverRepository.GetByUserIdAsync(userId);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> CreateAsync(CreateDriverDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            return Result<DriverResponseDto>.Fail("Usuario nao encontrado.");

        if (user.Role != UserRole.Driver)
            return Result<DriverResponseDto>.Fail("Usuario deve ter perfil Driver.");

        var existingDriver = await _driverRepository.GetByUserIdAsync(dto.UserId);

        if (existingDriver != null)
            return Result<DriverResponseDto>.Fail("Usuario ja possui cadastro de motorista.");

        if (await _driverRepository.CpfExistsAsync(dto.Cpf))
            return Result<DriverResponseDto>.Fail("CPF ja cadastrado.");

        if (await _driverRepository.CnhExistsAsync(dto.CnhNumber))
            return Result<DriverResponseDto>.Fail("CNH ja cadastrada.");

        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            BranchId = dto.BranchId ?? user.BranchId,
            Cpf = dto.Cpf.Trim(),
            Rg = dto.Rg.Trim(),
            BirthDate = ToUtc(dto.BirthDate),
            Phone = dto.Phone.Trim(),
            EmergencyPhone = dto.EmergencyPhone.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
            ZipCode = dto.ZipCode.Trim(),
            CnhNumber = dto.CnhNumber.Trim(),
            CnhCategory = dto.CnhCategory.Trim().ToUpperInvariant(),
            CnhExpiration = ToUtc(dto.CnhExpiration),
            Status = DriverStatus.Offline,
            ApprovalStatus = DriverApprovalStatus.Pending,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await _driverRepository.AddAsync(driver);
        await _driverRepository.SaveChangesAsync();

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> UpdateAsync(Guid id, UpdateDriverDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        var normalizedCpf = dto.Cpf.Trim();
        var normalizedCnh = dto.CnhNumber.Trim();

        if (await _driverRepository.CpfExistsAsync(normalizedCpf, id))
            return Result<DriverResponseDto>.Fail("CPF ja cadastrado.");

        if (await _driverRepository.CnhExistsAsync(normalizedCnh, id))
            return Result<DriverResponseDto>.Fail("CNH ja cadastrada.");

        driver.Cpf = normalizedCpf;
        driver.BranchId = dto.BranchId ?? driver.BranchId;
        driver.Rg = dto.Rg.Trim();
        driver.BirthDate = ToUtc(dto.BirthDate);
        driver.Phone = dto.Phone.Trim();
        driver.EmergencyPhone = dto.EmergencyPhone.Trim();
        driver.Address = dto.Address.Trim();
        driver.City = dto.City.Trim();
        driver.State = dto.State.Trim().ToUpperInvariant();
        driver.ZipCode = dto.ZipCode.Trim();
        driver.CnhNumber = normalizedCnh;
        driver.CnhCategory = dto.CnhCategory.Trim().ToUpperInvariant();
        driver.CnhExpiration = ToUtc(dto.CnhExpiration);
        driver.Active = dto.Active;

        if (!driver.Active)
            driver.Status = DriverStatus.Offline;

        driver.UpdatedAt = DateTime.UtcNow;

        await _driverRepository.UpdateAsync(driver);
        await _driverRepository.SaveChangesAsync();

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> UpdateStatusAsync(Guid id, UpdateDriverStatusDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        if (!driver.Active)
            return Result<DriverResponseDto>.Fail("Motorista inativo.");

        if (driver.ApprovalStatus != DriverApprovalStatus.Approved)
            return Result<DriverResponseDto>.Fail("Motorista ainda nao aprovado.");

        driver.Status = dto.Status;
        driver.UpdatedAt = DateTime.UtcNow;

        await _driverRepository.UpdateAsync(driver);
        await _driverRepository.SaveChangesAsync();

        if (dto.Status != DriverStatus.Online)
        {
            var location = await _locationRepository.GetByDriverIdAsync(driver.Id);

            if (location != null)
            {
                location.Online = false;
                location.UpdatedAt = DateTime.UtcNow;
                await _locationRepository.UpdateAsync(location);
                await _locationRepository.SaveChangesAsync();
            }
        }

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> SetApprovalAsync(Guid id, DriverApprovalDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        if (dto.ApprovalStatus == DriverApprovalStatus.Rejected &&
            string.IsNullOrWhiteSpace(dto.RejectReason))
            return Result<DriverResponseDto>.Fail("Informe o motivo da rejeicao.");

        driver.ApprovalStatus = dto.ApprovalStatus;
        driver.RejectReason = dto.ApprovalStatus == DriverApprovalStatus.Rejected
            ? dto.RejectReason?.Trim()
            : null;
        driver.Active = dto.ApprovalStatus != DriverApprovalStatus.Rejected;
        driver.Status = dto.ApprovalStatus == DriverApprovalStatus.Approved
            ? driver.Status
            : DriverStatus.Offline;
        driver.UpdatedAt = DateTime.UtcNow;

        await _driverRepository.UpdateAsync(driver);
        await _driverRepository.SaveChangesAsync();

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result<DriverResponseDto>> UpdateDocumentsAsync(
        Guid id,
        string? photoUrl,
        string? cnhFrontUrl,
        string? cnhBackUrl)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result<DriverResponseDto>.Fail("Motorista nao encontrado.");

        if (!string.IsNullOrWhiteSpace(photoUrl))
            driver.PhotoUrl = photoUrl;

        if (!string.IsNullOrWhiteSpace(cnhFrontUrl))
            driver.CnhFrontUrl = cnhFrontUrl;

        if (!string.IsNullOrWhiteSpace(cnhBackUrl))
            driver.CnhBackUrl = cnhBackUrl;

        driver.UpdatedAt = DateTime.UtcNow;

        await _driverRepository.UpdateAsync(driver);
        await _driverRepository.SaveChangesAsync();

        return Result<DriverResponseDto>.Ok(ToResponse(driver));
    }

    public async Task<Result> DeactivateAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id);

        if (driver == null)
            return Result.Fail("Motorista nao encontrado.");

        driver.Active = false;
        driver.Status = DriverStatus.Offline;
        driver.UpdatedAt = DateTime.UtcNow;

        await _driverRepository.UpdateAsync(driver);
        await _driverRepository.SaveChangesAsync();

        return Result.Ok("Motorista desativado com sucesso.");
    }

    private static DriverResponseDto ToResponse(Driver driver)
    {
        return new DriverResponseDto
        {
            Id = driver.Id,
            UserId = driver.UserId,
            BranchId = driver.BranchId,
            BranchName = driver.Branch?.Name ?? driver.User?.Branch?.Name ?? "",
            Name = driver.User?.Name ?? "",
            Email = driver.User?.Email ?? "",
            Phone = driver.Phone,
            Cpf = driver.Cpf,
            Rg = driver.Rg,
            BirthDate = driver.BirthDate,
            EmergencyPhone = driver.EmergencyPhone,
            Address = driver.Address,
            City = driver.City,
            State = driver.State,
            ZipCode = driver.ZipCode,
            Cnh = driver.CnhNumber,
            CnhCategory = driver.CnhCategory,
            CnhExpiration = driver.CnhExpiration,
            Status = driver.Status,
            ApprovalStatus = driver.ApprovalStatus,
            RejectReason = driver.RejectReason,
            Active = driver.Active,
            PhotoUrl = driver.PhotoUrl,
            CnhFrontUrl = driver.CnhFrontUrl,
            CnhBackUrl = driver.CnhBackUrl,
            CreatedAt = driver.CreatedAt,
            UpdatedAt = driver.UpdatedAt
        };
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
