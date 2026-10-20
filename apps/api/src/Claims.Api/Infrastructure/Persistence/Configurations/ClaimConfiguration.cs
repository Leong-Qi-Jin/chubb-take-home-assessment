using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims", table =>
        {
            table.HasCheckConstraint("CK_Claims_Type", "[Type] IN ('Motor', 'Property')");
            table.HasCheckConstraint(
                "CK_Claims_Status",
                "[Status] IN ('Submitted', 'UnderReview', 'AwaitingInformation', 'Settled', 'Rejected')");
            table.HasCheckConstraint(
                "CK_Claims_Assignment",
                "([Status] = 'Submitted' AND [AssignedOfficerId] IS NULL) OR "
                    + "([Status] <> 'Submitted' AND [AssignedOfficerId] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_Claims_EstimatedLiability",
                "[EstimatedLiability] IS NULL OR [EstimatedLiability] >= 0");
            table.HasCheckConstraint(
                "CK_Claims_SettledEstimate",
                "[Status] <> 'Settled' OR [EstimatedLiability] IS NOT NULL");
            table.HasCheckConstraint(
                "CK_Claims_CurrencyCode",
                "LEN([CurrencyCode]) = 3 AND [CurrencyCode] NOT LIKE '%[^A-Z]%'");
            table.HasCheckConstraint("CK_Claims_IncidentAt", "[IncidentAt] <= [SubmittedAt]");
        });

        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.Reference).HasMaxLength(50);
        builder.Property(claim => claim.Type).HasEnumName();
        builder.Property(claim => claim.Status).HasEnumName();
        builder.Property(claim => claim.PolicyNumber).HasMaxLength(50);
        builder.Property(claim => claim.IncidentLocation).HasMaxLength(500);
        builder.Property(claim => claim.IncidentDescription).HasMaxLength(2000);
        builder.Property(claim => claim.EstimatedLiability).HasPrecision(18, 2);
        builder.Property(claim => claim.CurrencyCode).IsFixedLength().HasMaxLength(3);
        builder.Property(claim => claim.Version).IsRowVersion();

        builder.HasIndex(claim => claim.Reference).IsUnique();
        builder.HasIndex(claim => new { claim.ClaimantId, claim.SubmittedAt });
        builder.HasIndex(claim => new { claim.Status, claim.SubmittedAt });
        builder.HasIndex(claim => new { claim.AssignedOfficerId, claim.Status, claim.UpdatedAt });

        builder.HasOne(claim => claim.Claimant)
            .WithMany(claimant => claimant.Claims)
            .HasForeignKey(claim => claim.ClaimantId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(claim => claim.AssignedOfficer)
            .WithMany(staffMember => staffMember.AssignedClaims)
            .HasForeignKey(claim => claim.AssignedOfficerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
