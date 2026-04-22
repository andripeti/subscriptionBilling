using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasConversion(id => id.Value, v => new CustomerId(v));
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();

        b.OwnsOne(c => c.Email, e =>
        {
            e.Property(p => p.Value).HasColumnName("Email").HasMaxLength(320).IsRequired();
            e.HasIndex(p => p.Value).IsUnique();
        });

        b.Ignore(c => c.DomainEvents);
    }
}
