using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasConversion(id => id.Value, v => new SubscriptionId(v));
        b.Property(s => s.CustomerId).HasConversion(id => id.Value, v => new CustomerId(v)).IsRequired();
        b.Property(s => s.Status).HasConversion<int>();
        b.Property(s => s.CreatedAt).IsRequired();
        b.Property(s => s.ActivatedAt);
        b.Property(s => s.CancelledAt);
        b.Property(s => s.NextBillingDate);

        b.OwnsOne(s => s.Plan, p =>
        {
            p.Property(x => x.Name).HasColumnName("PlanName").HasMaxLength(100).IsRequired();
            p.Property(x => x.Cycle).HasColumnName("BillingCycle").HasConversion<int>();
            p.OwnsOne(x => x.Price, m =>
            {
                m.Property(x => x.Amount).HasColumnName("PriceAmount").HasColumnType("decimal(18,2)").IsRequired();
                m.Property(x => x.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
            });
        });

        b.HasIndex(s => s.CustomerId);
        b.HasIndex(s => new { s.Status, s.NextBillingDate });
        b.Ignore(s => s.DomainEvents);
    }
}
