using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Infrastructure.Idempotency;

namespace SubscriptionBilling.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("IdempotencyRecords");
        b.HasKey(r => r.Key);
        b.Property(r => r.Key).HasMaxLength(200);
        b.Property(r => r.Result).IsRequired();
        b.Property(r => r.CreatedAt).IsRequired();
    }
}
