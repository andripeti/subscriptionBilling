using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Infrastructure.Outbox;

namespace SubscriptionBilling.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(m => m.Id);
        b.Property(m => m.Type).HasMaxLength(500).IsRequired();
        b.Property(m => m.Payload).IsRequired();
        b.HasIndex(m => m.ProcessedOn);
    }
}
