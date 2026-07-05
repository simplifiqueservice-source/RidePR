using RidePR.Application.Services;

namespace RidePR.Api.Services;

public class DispatchTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DispatchTimeoutWorker> _logger;

    public DispatchTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DispatchTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<DispatchService>();

                await dispatchService.ProcessTimeoutsAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar timeouts de dispatch.");
            }
        }
    }
}
