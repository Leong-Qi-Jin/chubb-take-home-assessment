using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class MotorClaimDetailConfiguration : IEntityTypeConfiguration<MotorClaimDetail>
{
    public void Configure(EntityTypeBuilder<MotorClaimDetail> builder)
    {
        builder.ToTable("MotorClaimDetails");
        builder.HasKey(detail => detail.ClaimId);
        builder.Property(detail => detail.VehicleRegistration).HasMaxLength(50);
        builder.HasOne(detail => detail.Claim)
            .WithOne(claim => claim.MotorDetail)
            .HasForeignKey<MotorClaimDetail>(detail => detail.ClaimId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PropertyClaimDetailConfiguration : IEntityTypeConfiguration<PropertyClaimDetail>
{
    public void Configure(EntityTypeBuilder<PropertyClaimDetail> builder)
    {
        builder.ToTable("PropertyClaimDetails");
        builder.HasKey(detail => detail.ClaimId);
        builder.Property(detail => detail.PropertyAddress).HasMaxLength(500);
        builder.HasOne(detail => detail.Claim)
            .WithOne(claim => claim.PropertyDetail)
            .HasForeignKey<PropertyClaimDetail>(detail => detail.ClaimId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
