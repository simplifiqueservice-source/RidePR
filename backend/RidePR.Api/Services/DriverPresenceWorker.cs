using Microsoft.EntityFrameworkCore;
using RidePR.Domain.Enums;
using RidePR.Infrastructure.Data;

namespace RidePR.Api.Services;

public class DriverPresenceWorker : BackgroundService
{
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromSeconds(45);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriverPresenceWorker> _logger;

    public DriverPresenceWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DriverPresenceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cutoff = DateTime.UtcNow.Subtract(PresenceTtl);

                var staleLocations = await context.DriverLocations
                    .Where(x => x.Online && x.UpdatedAt < cutoff)
                    .ToListAsync(stoppingToken);

                if (staleLocations.Count == 0)
                    continue;

                var staleDriverIds = staleLocations.Select(x => x.DriverId).ToList();
                var staleDrivers = await context.Drivers
                    .Where(x => staleDriverIds.Contains(x.Id) && x.Status == DriverStatus.Online)
                    .ToListAsync(stoppingToken);

                foreach (var location in staleLocations)
                {
                    location.Online = false;
                    location.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var driver in staleDrivers)
                {
                    driver.Status = DriverStatus.Offline;
                    driver.UpdatedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar presenca expirada de motoristas.");
            }
        }
    }
}
