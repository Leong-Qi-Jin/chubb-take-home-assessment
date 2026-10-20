using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class InformationRequestConfiguration : IEntityTypeConfiguration<InformationRequest>
{
    public void Configure(EntityTypeBuilder<InformationRequest> builder)
    {
        builder.ToTable("InformationRequests", table =>
        {
            table.HasCheckConstraint("CK_InformationRequests_Status", "[Status] IN ('Open', 'Answered', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_InformationRequests_State",
                "([Status] = 'Open' AND [ResponseText] IS NULL AND [RespondedAt] IS NULL "
                    + "AND [CancelledById] IS NULL AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL) OR "
                    + "([Status] = 'Answered' AND [ResponseText] IS NOT NULL AND [RespondedAt] IS NOT NULL "
                    + "AND [CancelledById] IS NULL AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL) OR "
                    + "([Status] = 'Cancelled' AND [ResponseText] IS NULL AND [RespondedAt] IS NULL "
                    + "AND [CancelledById] IS NOT NULL AND [CancelledAt] IS NOT NULL AND [CancellationReason] IS NOT NULL)");
        });

        builder.HasKey(request => request.Id);
        builder.HasAlternateKey(request => new { request.ClaimId, request.Id });
        builder.Property(request => request.RequestText).HasMaxLength(2000);
        builder.Property(request => request.Status).HasEnumName();
        builder.Property(request => request.ResponseText).HasMaxLength(2000);
        builder.Property(request => request.CancellationReason).HasMaxLength(500);

        builder.HasIndex(request => request.ClaimId)
            .IsUnique()
            .HasFilter("[Status] = 'Open'");

        builder.HasOne(request => request.Claim)
            .WithMany(claim => claim.InformationRequests)
            .HasForeignKey(request => request.ClaimId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(request => request.RequestedBy)
            .WithMany()
            .HasForeignKey(request => request.RequestedById)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(request => request.CancelledBy)
            .WithMany()
            .HasForeignKey(request => request.CancelledById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
