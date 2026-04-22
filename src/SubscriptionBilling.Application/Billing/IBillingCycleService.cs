namespace SubscriptionBilling.Application.Billing;

public interface IBillingCycleService
{
    Task<int> RunOnce(CancellationToken ct);
}
