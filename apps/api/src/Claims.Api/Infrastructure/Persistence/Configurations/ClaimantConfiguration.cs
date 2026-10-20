using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class ClaimantConfiguration : IEntityTypeConfiguration<Claimant>
{
    public void Configure(EntityTypeBuilder<Claimant> builder)
    {
        builder.ToTable("Claimants");
        builder.HasKey(claimant => claimant.Id);
        builder.Property(claimant => claimant.Email).HasMaxLength(320);
        builder.Property(claimant => claimant.FullName).HasMaxLength(200);
        builder.Property(claimant => claimant.Phone).HasMaxLength(50);
        builder.HasIndex(claimant => claimant.Email).IsUnique();
    }
}
