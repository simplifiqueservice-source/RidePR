using Microsoft.EntityFrameworkCore;
using RidePR.Application.Interfaces;
using RidePR.Domain.Entities;
using RidePR.Infrastructure.Data;

namespace RidePR.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly ApplicationDbContext _context;

    public WalletRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Wallets
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        return await _context.Wallets
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Wallet wallet)
    {
        await _context.Wallets.AddAsync(wallet);
    }

    public Task UpdateAsync(Wallet wallet)
    {
        _context.Wallets.Update(wallet);
        return Task.CompletedTask;
    }

    public async Task AddTransactionAsync(WalletTransaction transaction)
    {
        await _context.WalletTransactions.AddAsync(transaction);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
