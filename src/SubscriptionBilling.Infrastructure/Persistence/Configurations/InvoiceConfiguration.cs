using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasConversion(id => id.Value, v => new InvoiceId(v));
        b.Property(i => i.SubscriptionId).HasConversion(id => id.Value, v => new SubscriptionId(v)).IsRequired();
        b.Property(i => i.CustomerId).HasConversion(id => id.Value, v => new CustomerId(v)).IsRequired();
        b.Property(i => i.Status).HasConversion<int>();
        b.Property(i => i.PeriodStart).IsRequired();
        b.Property(i => i.PeriodEnd).IsRequired();
        b.Property(i => i.IssuedAt).IsRequired();
        b.Property(i => i.PaidAt);

        b.OwnsOne(i => i.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        b.HasIndex(i => i.CustomerId);
        b.HasIndex(i => i.SubscriptionId);
        b.Ignore(i => i.DomainEvents);
    }
}
