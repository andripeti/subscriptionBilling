using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Billing;

namespace SubscriptionBilling.Infrastructure.BackgroundJobs;

public sealed class BillingCycleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BillingCycleWorker> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var billing = scope.ServiceProvider.GetRequiredService<IBillingCycleService>();
                var generated = await billing.RunOnce(stoppingToken);
                if (generated > 0)
                    logger.LogInformation("Billing cycle generated {Count} invoices", generated);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Billing cycle tick failed");
            }
            await Task.Delay(TickInterval, stoppingToken);
        }
    }
}
