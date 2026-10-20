namespace Claims.Api.Domain.Claims;

public sealed class ClaimHistoryEntry
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public ClaimHistoryEventType EventType { get; set; }
    public required string Summary { get; set; }
    public ClaimHistoryActorType ActorType { get; set; }
    public Guid? ActorClaimantId { get; set; }
    public Guid? ActorStaffMemberId { get; set; }
    public Guid? InformationRequestId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public required Claim Claim { get; set; }
    public Claimant? ActorClaimant { get; set; }
    public StaffMember? ActorStaffMember { get; set; }
    public InformationRequest? InformationRequest { get; set; }
}
