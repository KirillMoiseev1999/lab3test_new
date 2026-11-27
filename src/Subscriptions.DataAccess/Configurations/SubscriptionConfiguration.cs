using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Frequency).IsRequired().HasMaxLength(7);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(8);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UserId, x.NextPaymentDate, x.Id });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Subscription_Price", "PriceMinor BETWEEN 1 AND 99999999999");
            t.HasCheckConstraint("CK_Subscription_Currency", "Currency IN ('RUB','USD','EUR')");
            t.HasCheckConstraint("CK_Subscription_Status", "Status IN ('active','paused','archived')");
            t.HasCheckConstraint("CK_Subscription_Frequency", "Frequency IN ('monthly','yearly')");
        });
    }
}
