using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Shared.Pagination;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class PassengerService
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IUserRepository _userRepository;

    public PassengerService(IPassengerRepository passengerRepository, IUserRepository userRepository)
    {
        _passengerRepository = passengerRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResult<PassengerResponseDto>> GetPagedAsync(PassengerQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var passengers = await _passengerRepository.GetPagedAsync(
            query.Search,
            query.Active,
            page,
            pageSize);

        var total = await _passengerRepository.CountAsync(
            query.Search,
            query.Active);

        return new PagedResult<PassengerResponseDto>
        {
            Items = passengers.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Result<PassengerResponseDto>> GetByIdAsync(Guid id)
    {
        var passenger = await _passengerRepository.GetByIdAsync(id);

        if (passenger == null)
            return Result<PassengerResponseDto>.Fail("Passageiro nao encontrado.");

        return Result<PassengerResponseDto>.Ok(ToResponse(passenger));
    }

    public async Task<Result<PassengerResponseDto>> GetByUserIdAsync(Guid userId)
    {
        var passenger = await _passengerRepository.GetByUserIdAsync(userId);

        if (passenger == null)
            return Result<PassengerResponseDto>.Fail("Passageiro nao encontrado.");

        return Result<PassengerResponseDto>.Ok(ToResponse(passenger));
    }

    public async Task<Result<PassengerResponseDto>> CreateAsync(CreatePassengerDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            return Result<PassengerResponseDto>.Fail("Usuario nao encontrado.");

        if (user.Role != UserRole.Passenger)
            return Result<PassengerResponseDto>.Fail("Usuario deve ter perfil Passenger.");

        var existingPassenger = await _passengerRepository.GetByUserIdAsync(dto.UserId);

        if (existingPassenger != null)
            return Result<PassengerResponseDto>.Fail("Usuario ja possui cadastro de passageiro.");

        if (await _passengerRepository.CpfExistsAsync(dto.Cpf))
            return Result<PassengerResponseDto>.Fail("CPF ja cadastrado.");

        var passenger = new Passenger
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            User = user,
            Cpf = dto.Cpf.Trim(),
            BirthDate = ToUtc(dto.BirthDate),
            Phone = dto.Phone.Trim(),
            EmergencyPhone = dto.EmergencyPhone.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim().ToUpperInvariant(),
            ZipCode = dto.ZipCode.Trim(),
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _passengerRepository.AddAsync(passenger);
        await _passengerRepository.AddHistoryAsync(CreateHistory(
            passenger.Id,
            PassengerHistoryType.Created,
            "Cadastro de passageiro criado."));
        await _passengerRepository.SaveChangesAsync();

        return Result<PassengerResponseDto>.Ok(ToResponse(passenger));
    }

    public async Task<Result<PassengerResponseDto>> UpdateAsync(Guid id, UpdatePassengerDto dto)
    {
        var passenger = await _passengerRepository.GetByIdAsync(id);

        if (passenger == null)
            return Result<PassengerResponseDto>.Fail("Passageiro nao encontrado.");

        var normalizedCpf = dto.Cpf.Trim();

        if (await _passengerRepository.CpfExistsAsync(normalizedCpf, id))
            return Result<PassengerResponseDto>.Fail("CPF ja cadastrado.");

        passenger.Cpf = normalizedCpf;
        passenger.BirthDate = ToUtc(dto.BirthDate);
        passenger.Phone = dto.Phone.Trim();
        passenger.EmergencyPhone = dto.EmergencyPhone.Trim();
        passenger.Address = dto.Address.Trim();
        passenger.City = dto.City.Trim();
        passenger.State = dto.State.Trim().ToUpperInvariant();
        passenger.ZipCode = dto.ZipCode.Trim();
        var wasActive = passenger.Active;

        passenger.Active = dto.Active;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _passengerRepository.UpdateAsync(passenger);
        await _passengerRepository.AddHistoryAsync(CreateHistory(
            passenger.Id,
            ResolveUpdateHistoryType(wasActive, passenger.Active),
            "Cadastro de passageiro atualizado."));
        await _passengerRepository.SaveChangesAsync();

        return Result<PassengerResponseDto>.Ok(ToResponse(passenger));
    }

    public async Task<Result> DeactivateAsync(Guid id)
    {
        var passenger = await _passengerRepository.GetByIdAsync(id);

        if (passenger == null)
            return Result.Fail("Passageiro nao encontrado.");

        passenger.Active = false;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _passengerRepository.UpdateAsync(passenger);
        await _passengerRepository.AddHistoryAsync(CreateHistory(
            passenger.Id,
            PassengerHistoryType.Deactivated,
            "Passageiro desativado por soft delete."));
        await _passengerRepository.SaveChangesAsync();

        return Result.Ok("Passageiro desativado com sucesso.");
    }

    public async Task<Result<IReadOnlyList<PassengerHistoryResponseDto>>> GetHistoryAsync(Guid id)
    {
        var passenger = await _passengerRepository.GetByIdAsync(id);

        if (passenger == null)
            return Result<IReadOnlyList<PassengerHistoryResponseDto>>.Fail("Passageiro nao encontrado.");

        var history = await _passengerRepository.GetHistoryAsync(id);

        return Result<IReadOnlyList<PassengerHistoryResponseDto>>.Ok(
            history.Select(ToHistoryResponse).ToList());
    }

    private static PassengerResponseDto ToResponse(Passenger passenger)
    {
        return new PassengerResponseDto
        {
            Id = passenger.Id,
            UserId = passenger.UserId,
            Name = passenger.User?.Name ?? "",
            Email = passenger.User?.Email ?? "",
            Cpf = passenger.Cpf,
            BirthDate = passenger.BirthDate,
            Phone = passenger.Phone,
            EmergencyPhone = passenger.EmergencyPhone,
            Address = passenger.Address,
            City = passenger.City,
            State = passenger.State,
            ZipCode = passenger.ZipCode,
            Active = passenger.Active,
            CreatedAt = passenger.CreatedAt,
            UpdatedAt = passenger.UpdatedAt
        };
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static PassengerHistory CreateHistory(
        Guid passengerId,
        PassengerHistoryType type,
        string description)
    {
        return new PassengerHistory
        {
            Id = Guid.NewGuid(),
            PassengerId = passengerId,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PassengerHistoryType ResolveUpdateHistoryType(bool wasActive, bool isActive)
    {
        if (!wasActive && isActive)
            return PassengerHistoryType.Reactivated;

        if (wasActive && !isActive)
            return PassengerHistoryType.Deactivated;

        return PassengerHistoryType.Updated;
    }

    private static PassengerHistoryResponseDto ToHistoryResponse(PassengerHistory history)
    {
        return new PassengerHistoryResponseDto
        {
            Id = history.Id,
            PassengerId = history.PassengerId,
            Type = history.Type.ToString(),
            Description = history.Description,
            CreatedAt = history.CreatedAt
        };
    }
}
