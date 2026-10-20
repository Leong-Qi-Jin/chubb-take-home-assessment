namespace Claims.Api.Domain.Claims;

public sealed class InformationRequest
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Guid RequestedById { get; set; }
    public required string RequestText { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public InformationRequestStatus Status { get; set; }
    public string? ResponseText { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Guid? CancelledById { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public required Claim Claim { get; set; }
    public required StaffMember RequestedBy { get; set; }
    public StaffMember? CancelledBy { get; set; }
}
