using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Billing;
using SubscriptionBilling.Application.Customers.Commands;
using SubscriptionBilling.Application.Invoices.Commands;
using SubscriptionBilling.Application.Invoices.Queries;
using SubscriptionBilling.Application.Subscriptions.Commands;
using SubscriptionBilling.Infrastructure.BackgroundJobs;
using SubscriptionBilling.Infrastructure.Idempotency;
using SubscriptionBilling.Infrastructure.Outbox;
using SubscriptionBilling.Infrastructure.Persistence;
using SubscriptionBilling.Infrastructure.Persistence.Repositories;
using SubscriptionBilling.Infrastructure.Time;

namespace SubscriptionBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, string dbName)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<BillingDbContext>((sp, opts) =>
        {
            opts.UseInMemoryDatabase(dbName);
            opts.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BillingDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();
        services.AddScoped<IBillingCycleService, BillingCycleService>();

        // CQRS handlers
        services.AddScoped<ICommandHandler<CreateCustomerCommand, Guid>, CreateCustomerHandler>();
        services.AddScoped<ICommandHandler<CreateSubscriptionCommand, CreateSubscriptionResult>, CreateSubscriptionHandler>();
        services.AddScoped<ICommandHandler<CancelSubscriptionCommand, Unit>, CancelSubscriptionHandler>();
        services.AddScoped<ICommandHandler<PayInvoiceCommand, Unit>, PayInvoiceHandler>();
        services.AddScoped<IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>, GetInvoicesHandler>();

        // Idempotent decorators
        services.AddScoped<ICommandHandler<Idempotent<CreateCustomerCommand, Guid>, Guid>,
            IdempotentCommandHandler<CreateCustomerCommand, Guid>>();
        services.AddScoped<ICommandHandler<Idempotent<CreateSubscriptionCommand, CreateSubscriptionResult>, CreateSubscriptionResult>,
            IdempotentCommandHandler<CreateSubscriptionCommand, CreateSubscriptionResult>>();
        services.AddScoped<ICommandHandler<Idempotent<PayInvoiceCommand, Unit>, Unit>,
            IdempotentCommandHandler<PayInvoiceCommand, Unit>>();

        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<BillingCycleWorker>();

        return services;
    }
}
