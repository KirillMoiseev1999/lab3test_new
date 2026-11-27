using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscriptions.DataAccess.Entities;
namespace Subscriptions.DataAccess.Configurations;

public sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NormalizedEmail).IsRequired().HasMaxLength(254);
        builder.HasIndex(x => new { x.NormalizedEmail, x.OccurredAtUtcTicks });
    }
}
