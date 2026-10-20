using System.Globalization;
using System.Text.Json;

namespace Claims.Api.Domain.Claims;

public sealed record ClaimSubmission(
    ClaimType Type,
    string PolicyNumber,
    DateTimeOffset IncidentAt,
    string IncidentLocation,
    string IncidentDescription,
    string? VehicleRegistration,
    string? PropertyAddress,
    string CurrencyCode);

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

    // The workflow methods leave new entities' IDs empty so EF Core assigns SQL Server-friendly sequential GUIDs when they are saved.
    public static Claim Submit(
        ClaimSubmission submission,
        Claimant claimant,
        string reference,
        DateTimeOffset now)
    {
        if (submission.IncidentAt > now)
        {
            throw new ArgumentException("The incident cannot be in the future.", nameof(submission));
        }

        var claim = new Claim
        {
            Reference = reference,
            ClaimantId = claimant.Id,
            Claimant = claimant,
            Type = submission.Type,
            Status = ClaimStatus.Submitted,
            PolicyNumber = submission.PolicyNumber,
            IncidentAt = submission.IncidentAt,
            IncidentLocation = submission.IncidentLocation,
            IncidentDescription = submission.IncidentDescription,
            CurrencyCode = submission.CurrencyCode,
            SubmittedAt = now,
            UpdatedAt = now,
        };

        // Exactly one detail record must match the claim type.
        switch (submission)
        {
            case { Type: ClaimType.Motor, VehicleRegistration: { } registration, PropertyAddress: null }:
                claim.MotorDetail = new MotorClaimDetail { Claim = claim, VehicleRegistration = registration };
                break;
            case { Type: ClaimType.Property, PropertyAddress: { } address, VehicleRegistration: null }:
                claim.PropertyDetail = new PropertyClaimDetail { Claim = claim, PropertyAddress = address };
                break;
            default:
                throw new ArgumentException("The claim details must match the claim type.", nameof(submission));
        }

        claim.AddHistory(ClaimHistoryEventType.Submitted, "Claim submitted.", now, claimant: claimant);
        return claim;
    }

    public void Take(StaffMember officer, DateTimeOffset now)
    {
        // Only Submitted claims are unassigned, so this also covers every later status.
        if (AssignedOfficerId is not null)
        {
            throw new ClaimWorkflowException(
                ClaimWorkflowError.ClaimAlreadyAssigned,
                "This claim has already been taken by an officer.");
        }

        AssignedOfficerId = officer.Id;
        AssignedOfficer = officer;
        Status = ClaimStatus.UnderReview;
        UpdatedAt = now;
        AddHistory(
            ClaimHistoryEventType.OfficerAssigned,
            $"Claim assigned to {officer.DisplayName}.",
            now,
            officer: officer);
    }

    public void UpdateEstimatedLiability(StaffMember officer, decimal amount, DateTimeOffset now)
    {
        EnsureAssignedTo(officer);
        EnsureStatus(
            "The estimate can only change while the claim is under review or awaiting information.",
            ClaimStatus.UnderReview,
            ClaimStatus.AwaitingInformation);
        EnsureValidAmount(amount);

        if (EstimatedLiability == amount)
        {
            return;
        }

        var previousAmount = EstimatedLiability;
        EstimatedLiability = amount;
        UpdatedAt = now;
        AddHistory(
            ClaimHistoryEventType.EstimatedLiabilityUpdated,
            $"Estimated liability set to {FormatMoney(amount)}.",
            now,
            officer: officer,
            details: JsonSerializer.Serialize(new { previousAmount, amount, currency = CurrencyCode }));
    }

    public InformationRequest RequestInformation(StaffMember officer, string requestText, DateTimeOffset now)
    {
        EnsureAssignedTo(officer);
        EnsureStatus("Information can only be requested while the claim is under review.", ClaimStatus.UnderReview);

        var request = new InformationRequest
        {
            ClaimId = Id,
            Claim = this,
            RequestedById = officer.Id,
            RequestedBy = officer,
            RequestText = requestText,
            RequestedAt = now,
            Status = InformationRequestStatus.Open,
        };
        InformationRequests.Add(request);
        Status = ClaimStatus.AwaitingInformation;
        UpdatedAt = now;
        AddHistory(
            ClaimHistoryEventType.InformationRequested,
            "Additional information requested.",
            now,
            officer: officer,
            request: request);
        return request;
    }

    public void ProvideInformation(Guid requestId, string responseText, DateTimeOffset now)
    {
        var request = GetOpenInformationRequest(requestId);
        request.Status = InformationRequestStatus.Answered;
        request.ResponseText = responseText;
        request.RespondedAt = now;
        Status = ClaimStatus.UnderReview;
        UpdatedAt = now;
        AddHistory(
            ClaimHistoryEventType.InformationProvided,
            "Additional information provided.",
            now,
            claimant: Claimant,
            request: request);
    }

    public void CancelInformationRequest(
        StaffMember officer,
        Guid requestId,
        string cancellationReason,
        DateTimeOffset now)
    {
        EnsureAssignedTo(officer);
        var request = GetOpenInformationRequest(requestId);
        request.Status = InformationRequestStatus.Cancelled;
        request.CancelledById = officer.Id;
        request.CancelledBy = officer;
        request.CancelledAt = now;
        request.CancellationReason = cancellationReason;
        Status = ClaimStatus.UnderReview;
        UpdatedAt = now;
        AddHistory(
            ClaimHistoryEventType.InformationRequestCancelled,
            "Information request cancelled.",
            now,
            officer: officer,
            request: request);
    }

    public void Settle(StaffMember officer, decimal settlementAmount, string rationale, DateTimeOffset now)
    {
        EnsureAssignedTo(officer);
        EnsureStatus("Only a claim under review can be settled.", ClaimStatus.UnderReview);
        EnsureValidAmount(settlementAmount);

        if (EstimatedLiability is null)
        {
            throw new ClaimWorkflowException(
                ClaimWorkflowError.EstimatedLiabilityRequired,
                "Record an estimated liability before settling the claim.");
        }

        Decide(officer, ClaimDecisionOutcome.Settled, settlementAmount, rationale, now);
        AddHistory(
            ClaimHistoryEventType.Settled,
            $"Claim settled for {FormatMoney(settlementAmount)}.",
            now,
            officer: officer);
    }

    public void Reject(StaffMember officer, string rationale, DateTimeOffset now)
    {
        EnsureAssignedTo(officer);
        EnsureStatus("Only a claim under review can be rejected.", ClaimStatus.UnderReview);

        Decide(officer, ClaimDecisionOutcome.Rejected, settlementAmount: null, rationale, now);
        AddHistory(ClaimHistoryEventType.Rejected, "Claim rejected.", now, officer: officer);
    }

    private void Decide(
        StaffMember officer,
        ClaimDecisionOutcome outcome,
        decimal? settlementAmount,
        string rationale,
        DateTimeOffset now)
    {
        Decision = new ClaimDecision
        {
            ClaimId = Id,
            Claim = this,
            DecidedById = officer.Id,
            DecidedBy = officer,
            Outcome = outcome,
            SettlementAmount = settlementAmount,
            Rationale = rationale,
            DecidedAt = now,
        };
        Status = outcome == ClaimDecisionOutcome.Settled ? ClaimStatus.Settled : ClaimStatus.Rejected;
        UpdatedAt = now;
    }

    private void EnsureAssignedTo(StaffMember officer)
    {
        if (AssignedOfficerId != officer.Id)
        {
            throw new ClaimWorkflowException(
                ClaimWorkflowError.NotAssignedOfficer,
                "Only the officer assigned to this claim can do this.");
        }
    }

    private void EnsureStatus(string message, params ClaimStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
        {
            throw new ClaimWorkflowException(ClaimWorkflowError.ClaimStatusNotAllowed, message);
        }
    }

    private InformationRequest GetOpenInformationRequest(Guid requestId)
    {
        var request = InformationRequests.SingleOrDefault(request => request.Id == requestId)
            ?? throw new ClaimWorkflowException(
                ClaimWorkflowError.InformationRequestNotFound,
                "The information request was not found on this claim.");

        if (request.Status != InformationRequestStatus.Open)
        {
            throw new ClaimWorkflowException(
                ClaimWorkflowError.InformationRequestNotOpen,
                "The information request has already been answered or cancelled.");
        }

        return request;
    }

    private static void EnsureValidAmount(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Amounts can have at most two decimal places.", nameof(amount));
        }
    }

    private string FormatMoney(decimal amount) =>
        $"{CurrencyCode} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    private void AddHistory(
        ClaimHistoryEventType eventType,
        string summary,
        DateTimeOffset occurredAt,
        Claimant? claimant = null,
        StaffMember? officer = null,
        InformationRequest? request = null,
        string? details = null) =>
        HistoryEntries.Add(new ClaimHistoryEntry
        {
            ClaimId = Id,
            Claim = this,
            EventType = eventType,
            Summary = summary,
            ActorType = claimant is not null ? ClaimHistoryActorType.Claimant : ClaimHistoryActorType.ClaimsOfficer,
            ActorClaimantId = claimant?.Id,
            ActorClaimant = claimant,
            ActorStaffMemberId = officer?.Id,
            ActorStaffMember = officer,
            InformationRequest = request,
            Details = details,
            OccurredAt = occurredAt,
        });
}
