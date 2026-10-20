using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class ClaimDecisionConfiguration : IEntityTypeConfiguration<ClaimDecision>
{
    public void Configure(EntityTypeBuilder<ClaimDecision> builder)
    {
        builder.ToTable("ClaimDecisions", table =>
        {
            table.HasCheckConstraint("CK_ClaimDecisions_Outcome", "[Outcome] IN ('Settled', 'Rejected')");
            table.HasCheckConstraint(
                "CK_ClaimDecisions_SettlementAmount",
                "([Outcome] = 'Settled' AND [SettlementAmount] IS NOT NULL AND [SettlementAmount] >= 0) OR "
                    + "([Outcome] = 'Rejected' AND [SettlementAmount] IS NULL)");
        });

        builder.HasKey(decision => decision.ClaimId);
        builder.Property(decision => decision.Outcome).HasEnumName();
        builder.Property(decision => decision.SettlementAmount).HasPrecision(18, 2);
        builder.Property(decision => decision.Rationale).HasMaxLength(2000);

        builder.HasOne(decision => decision.Claim)
            .WithOne(claim => claim.Decision)
            .HasForeignKey<ClaimDecision>(decision => decision.ClaimId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(decision => decision.DecidedBy)
            .WithMany()
            .HasForeignKey(decision => decision.DecidedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
