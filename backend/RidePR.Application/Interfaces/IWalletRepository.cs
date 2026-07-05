using RidePR.Domain.Entities;

namespace RidePR.Application.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId);

    Task<Wallet?> GetByIdAsync(Guid id);

    Task AddAsync(Wallet wallet);

    Task UpdateAsync(Wallet wallet);

    Task AddTransactionAsync(WalletTransaction transaction);

    Task SaveChangesAsync();
}
