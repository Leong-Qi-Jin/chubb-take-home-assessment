namespace Claims.Api.Domain.Claims;

public sealed class Claim
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public Guid ClaimantId { get; set; }
    public ClaimType Type { get; set; }
    public ClaimStatus Status { get; set; }
    public required string PolicyNumber { get; set; }
    public DateTimeOffset IncidentAt { get; set; }
    public required string IncidentLocation { get; set; }
    public required string IncidentDescription { get; set; }
    public Guid? AssignedOfficerId { get; set; }
    public decimal? EstimatedLiability { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];

    public required Claimant Claimant { get; set; }
    public StaffMember? AssignedOfficer { get; set; }
    public MotorClaimDetail? MotorDetail { get; set; }
    public PropertyClaimDetail? PropertyDetail { get; set; }
    public ICollection<InformationRequest> InformationRequests { get; } = [];
    public ClaimDecision? Decision { get; set; }
    public ICollection<ClaimHistoryEntry> HistoryEntries { get; } = [];
}
