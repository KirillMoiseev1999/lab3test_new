using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.HasOne<Subscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SubscriptionId, x.PaidOn });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Payment_Amount", "AmountMinor BETWEEN 1 AND 99999999999");
            t.HasCheckConstraint("CK_Payment_Currency", "Currency IN ('RUB','USD','EUR')");
        });
    }
}
