using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Domain.Enums;
using RidePR.Shared.Pagination;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class PaymentService
{
    private const decimal DriverSplitPercentage = 0.80m;
    private const decimal PlatformSplitPercentage = 0.20m;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentGateway _gateway;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IWalletRepository walletRepository,
        ITripRepository tripRepository,
        IPassengerRepository passengerRepository,
        IDriverRepository driverRepository,
        IUserRepository userRepository,
        IPaymentGateway gateway)
    {
        _paymentRepository = paymentRepository;
        _walletRepository = walletRepository;
        _tripRepository = tripRepository;
        _passengerRepository = passengerRepository;
        _driverRepository = driverRepository;
        _userRepository = userRepository;
        _gateway = gateway;
    }

    public async Task<PagedResult<PaymentResponseDto>> GetPagedAsync(PaymentQueryDto query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var payments = await _paymentRepository.GetPagedAsync(
            query.TripId,
            query.PassengerId,
            query.DriverId,
            query.Method,
            query.Status,
            page,
            pageSize);

        var total = await _paymentRepository.CountAsync(
            query.TripId,
            query.PassengerId,
            query.DriverId,
            query.Method,
            query.Status);

        return new PagedResult<PaymentResponseDto>
        {
            Items = payments.Select(ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Result<PaymentResponseDto>> GetByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return Result<PaymentResponseDto>.Fail("Pagamento nao encontrado.");

        return Result<PaymentResponseDto>.Ok(ToResponse(payment));
    }

    public async Task<Result<PaymentResponseDto>> CreateAsync(CreatePaymentDto dto)
    {
        var trip = await _tripRepository.GetByIdAsync(dto.TripId);

        if (trip == null)
            return Result<PaymentResponseDto>.Fail("Corrida nao encontrada.");

        var passenger = await _passengerRepository.GetByIdAsync(dto.PassengerId);

        if (passenger == null || !passenger.Active)
            return Result<PaymentResponseDto>.Fail("Passageiro nao encontrado ou inativo.");

        if (trip.PassengerId != dto.PassengerId)
            return Result<PaymentResponseDto>.Fail("Passageiro nao pertence a corrida.");

        var amount = dto.Amount > 0 ? dto.Amount : trip.Price;

        if (amount <= 0)
            return Result<PaymentResponseDto>.Fail("Valor do pagamento invalido.");

        var existingPayment = await _paymentRepository.GetByTripIdAsync(dto.TripId);

        if (existingPayment != null && existingPayment.Status != PaymentStatus.Cancelled && existingPayment.Status != PaymentStatus.Failed)
            return Result<PaymentResponseDto>.Fail("Corrida ja possui pagamento ativo.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TripId = dto.TripId,
            PassengerId = dto.PassengerId,
            DriverId = dto.DriverId ?? trip.DriverId,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            Amount = amount,
            Currency = "BRL",
            Provider = string.IsNullOrWhiteSpace(dto.Provider) ? "RidePR" : dto.Provider,
            CreatedAt = DateTime.UtcNow
        };

        var result = dto.Method switch
        {
            PaymentMethod.Pix => await CreatePixAsync(payment),
            PaymentMethod.CreditCard or PaymentMethod.DebitCard => await ChargeCardAsync(payment, dto.Card),
            PaymentMethod.Wallet => await ChargeWalletAsync(payment, passenger.UserId),
            _ => Result<Payment>.Fail("Metodo de pagamento invalido.")
        };

        if (!result.Success || result.Data == null)
            return Result<PaymentResponseDto>.Fail(result.Message);

        await _paymentRepository.AddAsync(result.Data);
        await _paymentRepository.SaveChangesAsync();

        return Result<PaymentResponseDto>.Ok(ToResponse(result.Data));
    }

    public async Task<Result<PaymentResponseDto>> ConfirmPixAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return Result<PaymentResponseDto>.Fail("Pagamento nao encontrado.");

        if (payment.Method != PaymentMethod.Pix)
            return Result<PaymentResponseDto>.Fail("Pagamento nao e PIX.");

        if (payment.Status != PaymentStatus.Pending)
            return Result<PaymentResponseDto>.Fail("Pagamento nao esta pendente.");

        MarkPaid(payment);

        await _paymentRepository.UpdateAsync(payment);
        await _paymentRepository.SaveChangesAsync();

        return Result<PaymentResponseDto>.Ok(ToResponse(payment));
    }

    public async Task<Result<PaymentResponseDto>> RefundAsync(Guid id, RefundPaymentDto dto)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
            return Result<PaymentResponseDto>.Fail("Pagamento nao encontrado.");

        if (payment.Status != PaymentStatus.Paid && payment.Status != PaymentStatus.PartiallyRefunded)
            return Result<PaymentResponseDto>.Fail("Pagamento nao permite estorno.");

        var refundableAmount = payment.Amount - payment.RefundedAmount;

        if (dto.Amount <= 0 || dto.Amount > refundableAmount)
            return Result<PaymentResponseDto>.Fail("Valor de estorno invalido.");

        var gatewayResult = await _gateway.RefundAsync(payment, dto.Amount, dto.Reason);

        if (!gatewayResult.Success)
            return Result<PaymentResponseDto>.Fail(gatewayResult.FailureReason ?? "Falha ao estornar pagamento.");

        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Amount = dto.Amount,
            Reason = dto.Reason.Trim(),
            Status = RefundStatus.Completed,
            ProviderRefundId = gatewayResult.ProviderRefundId,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        payment.Refunds.Add(refund);
        payment.RefundedAmount += dto.Amount;
        payment.Status = payment.RefundedAmount >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        payment.UpdatedAt = DateTime.UtcNow;

        var passenger = await _passengerRepository.GetByIdAsync(payment.PassengerId);

        if (passenger != null)
            await CreditWalletAsync(passenger.UserId, dto.Amount, "Estorno de pagamento.", payment.Id, WalletTransactionType.Refund);

        await _paymentRepository.UpdateAsync(payment);
        await _paymentRepository.SaveChangesAsync();

        return Result<PaymentResponseDto>.Ok(ToResponse(payment));
    }

    public async Task<Result<WalletResponseDto>> GetWalletAsync(Guid userId)
    {
        var wallet = await GetOrCreateWalletAsync(userId);

        return Result<WalletResponseDto>.Ok(ToWalletResponse(wallet));
    }

    public async Task<Result<WalletResponseDto>> CreditWalletAsync(WalletCreditDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
            return Result<WalletResponseDto>.Fail("Usuario nao encontrado.");

        var wallet = await CreditWalletAsync(dto.UserId, dto.Amount, dto.Description, null, WalletTransactionType.Credit);

        await _walletRepository.SaveChangesAsync();

        return Result<WalletResponseDto>.Ok(ToWalletResponse(wallet));
    }

    private async Task<Result<Payment>> CreatePixAsync(Payment payment)
    {
        var gatewayResult = await _gateway.CreatePixAsync(payment);

        if (!gatewayResult.Success)
            return Result<Payment>.Fail(gatewayResult.FailureReason ?? "Falha ao gerar PIX.");

        payment.ProviderPaymentId = gatewayResult.ProviderPaymentId;
        payment.PixQrCode = gatewayResult.PixQrCode;
        payment.PixCopyPaste = gatewayResult.PixCopyPaste;
        payment.PixExpiresAt = gatewayResult.PixExpiresAt;

        return Result<Payment>.Ok(payment);
    }

    private async Task<Result<Payment>> ChargeCardAsync(Payment payment, CardPaymentDto? card)
    {
        if (card == null)
            return Result<Payment>.Fail("Dados do cartao nao informados.");

        var gatewayResult = await _gateway.ChargeCardAsync(payment, card);

        if (!gatewayResult.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = gatewayResult.FailureReason;
            return Result<Payment>.Fail(gatewayResult.FailureReason ?? "Falha ao cobrar cartao.");
        }

        payment.ProviderPaymentId = gatewayResult.ProviderPaymentId;
        payment.CardLast4 = card.Last4.Trim();
        payment.CardBrand = card.Brand.Trim();
        MarkPaid(payment);

        return Result<Payment>.Ok(payment);
    }

    private async Task<Result<Payment>> ChargeWalletAsync(Payment payment, Guid passengerUserId)
    {
        var wallet = await GetOrCreateWalletAsync(passengerUserId);

        if (wallet.Balance < payment.Amount)
            return Result<Payment>.Fail("Saldo insuficiente na carteira.");

        wallet.Balance -= payment.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepository.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            PaymentId = payment.Id,
            Type = WalletTransactionType.Debit,
            Amount = payment.Amount,
            BalanceAfter = wallet.Balance,
            Description = "Pagamento de corrida.",
            CreatedAt = DateTime.UtcNow
        });

        await _walletRepository.UpdateAsync(wallet);

        MarkPaid(payment);

        return Result<Payment>.Ok(payment);
    }

    private void MarkPaid(Payment payment)
    {
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        CreateSplits(payment);
    }

    private void CreateSplits(Payment payment)
    {
        if (payment.Splits.Count > 0)
            return;

        var driverAmount = Math.Round(payment.Amount * DriverSplitPercentage, 2);
        var platformAmount = payment.Amount - driverAmount;

        payment.Splits.Add(new PaymentSplit
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            DriverId = payment.DriverId,
            RecipientType = "Driver",
            Amount = driverAmount,
            Percentage = DriverSplitPercentage * 100,
            Settled = false,
            CreatedAt = DateTime.UtcNow
        });

        payment.Splits.Add(new PaymentSplit
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            RecipientType = "Platform",
            Amount = platformAmount,
            Percentage = PlatformSplitPercentage * 100,
            Settled = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<Wallet> CreditWalletAsync(
        Guid userId,
        decimal amount,
        string description,
        Guid? paymentId,
        WalletTransactionType type)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepository.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            PaymentId = paymentId,
            Type = type,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await _walletRepository.UpdateAsync(wallet);

        return wallet;
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);

        if (wallet != null)
            return wallet;

        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();

        return wallet;
    }

    private static PaymentResponseDto ToResponse(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            TripId = payment.TripId,
            PassengerId = payment.PassengerId,
            DriverId = payment.DriverId,
            Method = payment.Method,
            Status = payment.Status,
            Amount = payment.Amount,
            RefundedAmount = payment.RefundedAmount,
            Currency = payment.Currency,
            Provider = payment.Provider,
            ProviderPaymentId = payment.ProviderPaymentId,
            PixQrCode = payment.PixQrCode,
            PixCopyPaste = payment.PixCopyPaste,
            PixExpiresAt = payment.PixExpiresAt,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand,
            FailureReason = payment.FailureReason,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            Splits = payment.Splits.Select(x => new PaymentSplitResponseDto
            {
                Id = x.Id,
                DriverId = x.DriverId,
                RecipientType = x.RecipientType,
                Amount = x.Amount,
                Percentage = x.Percentage,
                Settled = x.Settled
            }).ToList(),
            Refunds = payment.Refunds.Select(x => new PaymentRefundResponseDto
            {
                Id = x.Id,
                Amount = x.Amount,
                Reason = x.Reason,
                Status = x.Status,
                ProviderRefundId = x.ProviderRefundId,
                CreatedAt = x.CreatedAt,
                CompletedAt = x.CompletedAt
            }).ToList()
        };
    }

    private static WalletResponseDto ToWalletResponse(Wallet wallet)
    {
        return new WalletResponseDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            Active = wallet.Active,
            CreatedAt = wallet.CreatedAt
        };
    }
}
