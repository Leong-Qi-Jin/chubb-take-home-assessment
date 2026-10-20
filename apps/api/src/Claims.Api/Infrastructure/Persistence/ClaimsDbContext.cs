using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Infrastructure.Persistence;

public sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Claimant> Claimants => Set<Claimant>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<MotorClaimDetail> MotorClaimDetails => Set<MotorClaimDetail>();
    public DbSet<PropertyClaimDetail> PropertyClaimDetails => Set<PropertyClaimDetail>();
    public DbSet<InformationRequest> InformationRequests => Set<InformationRequest>();
    public DbSet<ClaimDecision> ClaimDecisions => Set<ClaimDecision>();
    public DbSet<ClaimHistoryEntry> ClaimHistoryEntries => Set<ClaimHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
}
