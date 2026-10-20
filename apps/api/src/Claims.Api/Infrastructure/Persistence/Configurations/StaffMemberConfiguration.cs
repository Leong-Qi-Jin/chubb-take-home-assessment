using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Api.Infrastructure.Persistence.Configurations;

public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("StaffMembers");
        builder.HasKey(staffMember => staffMember.Id);
        builder.Property(staffMember => staffMember.Email).HasMaxLength(320);
        builder.Property(staffMember => staffMember.DisplayName).HasMaxLength(200);
        builder.HasIndex(staffMember => staffMember.Email).IsUnique();
    }
}
