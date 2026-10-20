namespace Claims.Api.Domain.Claims;

public sealed class ClaimDecision
{
    public Guid ClaimId { get; set; }
    public Guid DecidedById { get; set; }
    public ClaimDecisionOutcome Outcome { get; set; }
    public decimal? SettlementAmount { get; set; }
    public required string Rationale { get; set; }
    public DateTimeOffset DecidedAt { get; set; }

    public required Claim Claim { get; set; }
    public required StaffMember DecidedBy { get; set; }
}
