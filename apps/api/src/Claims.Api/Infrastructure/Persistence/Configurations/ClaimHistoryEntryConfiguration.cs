using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class ClaimHistoryEntryConfiguration : IEntityTypeConfiguration<ClaimHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ClaimHistoryEntry> builder)
    {
        builder.ToTable("ClaimHistoryEntries", table =>
        {
            table.HasCheckConstraint(
                "CK_ClaimHistoryEntries_EventType",
                "[EventType] IN ('Submitted', 'OfficerAssigned', 'EstimatedLiabilityUpdated', "
                    + "'InformationRequested', 'InformationProvided', 'InformationRequestCancelled', 'Settled', 'Rejected')");
            table.HasCheckConstraint(
                "CK_ClaimHistoryEntries_ActorType",
                "[ActorType] IN ('Claimant', 'ClaimsOfficer', 'System')");
            table.HasCheckConstraint(
                "CK_ClaimHistoryEntries_Actor",
                "([ActorType] = 'Claimant' AND [ActorClaimantId] IS NOT NULL AND [ActorStaffMemberId] IS NULL) OR "
                    + "([ActorType] = 'ClaimsOfficer' AND [ActorClaimantId] IS NULL AND [ActorStaffMemberId] IS NOT NULL) OR "
                    + "([ActorType] = 'System' AND [ActorClaimantId] IS NULL AND [ActorStaffMemberId] IS NULL)");
            table.HasCheckConstraint(
                "CK_ClaimHistoryEntries_InformationRequest",
                "([EventType] IN ('InformationRequested', 'InformationProvided', 'InformationRequestCancelled') "
                    + "AND [InformationRequestId] IS NOT NULL) OR "
                    + "([EventType] NOT IN ('InformationRequested', 'InformationProvided', 'InformationRequestCancelled') "
                    + "AND [InformationRequestId] IS NULL)");
        });

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.EventType).HasEnumName();
        builder.Property(entry => entry.Summary).HasMaxLength(500);
        builder.Property(entry => entry.ActorType).HasEnumName();
        builder.Property(entry => entry.Details).HasColumnType("json");
        builder.HasIndex(entry => new { entry.ClaimId, entry.OccurredAt });

        builder.HasOne(entry => entry.Claim)
            .WithMany(claim => claim.HistoryEntries)
            .HasForeignKey(entry => entry.ClaimId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entry => entry.ActorClaimant)
            .WithMany()
            .HasForeignKey(entry => entry.ActorClaimantId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entry => entry.ActorStaffMember)
            .WithMany()
            .HasForeignKey(entry => entry.ActorStaffMemberId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(entry => entry.InformationRequest)
            .WithMany()
            .HasForeignKey(entry => new { entry.ClaimId, entry.InformationRequestId })
            .HasPrincipalKey(request => new { request.ClaimId, request.Id })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
