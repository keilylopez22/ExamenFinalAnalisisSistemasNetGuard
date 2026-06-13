using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.BackgroundServices;

public class EscalationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<EscalationBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IncidentService>();
                await service.EscalateOverdueAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during auto-escalation check.");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
