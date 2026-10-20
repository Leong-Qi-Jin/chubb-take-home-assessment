using System.Text.Json;
using Claims.Api.Domain.Claims;

namespace Claims.Api.UnitTests.Domain.Claims;

public sealed class ClaimWorkflowTests
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Claimant claimant = new()
    {
        Id = Guid.NewGuid(),
        Email = "alex.tan@example.com",
        FullName = "Alex Tan",
    };

    private readonly StaffMember officer = new()
    {
        Id = Guid.NewGuid(),
        Email = "sam.lee@claims.example",
        DisplayName = "Sam Lee",
        IsActive = true,
    };

    private readonly StaffMember otherOfficer = new()
    {
        Id = Guid.NewGuid(),
        Email = "aisha.khan@claims.example",
        DisplayName = "Aisha Khan",
        IsActive = true,
    };

    [Fact]
    public void Submit_creates_a_submitted_claim_with_matching_details_and_history()
    {
        var claim = Submit(ClaimType.Property);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Null(claim.MotorDetail);
        Assert.Equal("12 Example Street, Kuala Lumpur", claim.PropertyDetail?.PropertyAddress);
        var entry = Assert.Single(claim.HistoryEntries);
        Assert.Equal(ClaimHistoryEventType.Submitted, entry.EventType);
        Assert.Equal(ClaimHistoryActorType.Claimant, entry.ActorType);
        Assert.Equal(claimant.Id, entry.ActorClaimantId);
    }

    [Fact]
    public void Take_assigns_the_officer_and_starts_review_only_once()
    {
        var claim = Submit();

        claim.Take(officer, SubmittedAt.AddHours(1));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(officer.Id, claim.AssignedOfficerId);
        Assert.Equal(SubmittedAt.AddHours(1), claim.UpdatedAt);
        AssertWorkflowError(ClaimWorkflowError.ClaimAlreadyAssigned, () => claim.Take(otherOfficer, SubmittedAt.AddHours(2)));
    }

    [Fact]
    public void Only_the_assigned_officer_can_act_on_a_claim()
    {
        var claim = TakenClaim();

        AssertWorkflowError(
            ClaimWorkflowError.NotAssignedOfficer,
            () => claim.UpdateEstimatedLiability(otherOfficer, 100m, SubmittedAt.AddHours(2)));
        AssertWorkflowError(
            ClaimWorkflowError.NotAssignedOfficer,
            () => claim.Reject(otherOfficer, "Not covered.", SubmittedAt.AddHours(2)));
    }

    [Fact]
    public void Estimate_changes_record_the_previous_amount_and_ignore_unchanged_values()
    {
        var claim = TakenClaim();

        claim.UpdateEstimatedLiability(officer, 4200m, SubmittedAt.AddHours(2));
        claim.UpdateEstimatedLiability(officer, 3900.5m, SubmittedAt.AddHours(3));
        claim.UpdateEstimatedLiability(officer, 3900.5m, SubmittedAt.AddHours(4));

        Assert.Equal(3900.5m, claim.EstimatedLiability);
        Assert.Equal(SubmittedAt.AddHours(3), claim.UpdatedAt);
        var latest = claim.HistoryEntries.Last(entry => entry.EventType == ClaimHistoryEventType.EstimatedLiabilityUpdated);
        Assert.Equal("Estimated liability set to SGD 3,900.50.", latest.Summary);
        using var details = JsonDocument.Parse(latest.Details!);
        Assert.Equal(4200m, details.RootElement.GetProperty("previousAmount").GetDecimal());
        Assert.Equal(2, claim.HistoryEntries.Count(entry => entry.EventType == ClaimHistoryEventType.EstimatedLiabilityUpdated));
    }

    [Fact]
    public void Answering_an_information_request_returns_the_claim_to_review()
    {
        var claim = TakenClaim();
        var request = claim.RequestInformation(officer, "Please send the police report number.", SubmittedAt.AddHours(2));

        Assert.Equal(ClaimStatus.AwaitingInformation, claim.Status);
        AssertWorkflowError(
            ClaimWorkflowError.ClaimStatusNotAllowed,
            () => claim.RequestInformation(officer, "And photos.", SubmittedAt.AddHours(3)));

        // The estimate stays editable while waiting for the claimant.
        claim.UpdateEstimatedLiability(officer, 1200m, SubmittedAt.AddHours(3));
        claim.ProvideInformation(request.Id, "Report SPF-2026-1234.", SubmittedAt.AddHours(4));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(InformationRequestStatus.Answered, request.Status);
        Assert.Equal("Report SPF-2026-1234.", request.ResponseText);
        var entry = claim.HistoryEntries.Last();
        Assert.Equal(ClaimHistoryEventType.InformationProvided, entry.EventType);
        Assert.Equal(ClaimHistoryActorType.Claimant, entry.ActorType);
        Assert.Same(request, entry.InformationRequest);
    }

    [Fact]
    public void A_closed_or_unknown_information_request_cannot_be_answered_or_cancelled()
    {
        var claim = TakenClaim();
        var request = claim.RequestInformation(officer, "Please send photos.", SubmittedAt.AddHours(2));
        // EF Core assigns IDs on save; give the request one so it can be addressed like a persisted request.
        request.Id = Guid.NewGuid();

        claim.CancelInformationRequest(officer, request.Id, "Found in the original report.", SubmittedAt.AddHours(3));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(officer.Id, request.CancelledById);
        AssertWorkflowError(
            ClaimWorkflowError.InformationRequestNotOpen,
            () => claim.ProvideInformation(request.Id, "Photos attached.", SubmittedAt.AddHours(4)));
        AssertWorkflowError(
            ClaimWorkflowError.InformationRequestNotFound,
            () => claim.CancelInformationRequest(officer, Guid.NewGuid(), "Duplicate.", SubmittedAt.AddHours(4)));
    }

    [Fact]
    public void Settling_requires_an_estimate_and_records_a_final_decision()
    {
        var claim = TakenClaim();

        AssertWorkflowError(
            ClaimWorkflowError.EstimatedLiabilityRequired,
            () => claim.Settle(officer, 1000m, "Covered.", SubmittedAt.AddHours(2)));

        claim.UpdateEstimatedLiability(officer, 1200m, SubmittedAt.AddHours(2));
        claim.Settle(officer, 1000m, "Covered under section 2.", SubmittedAt.AddHours(3));

        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(ClaimDecisionOutcome.Settled, claim.Decision?.Outcome);
        Assert.Equal(1000m, claim.Decision?.SettlementAmount);
        Assert.Equal("Claim settled for SGD 1,000.00.", claim.HistoryEntries.Last().Summary);
        AssertWorkflowError(
            ClaimWorkflowError.ClaimStatusNotAllowed,
            () => claim.UpdateEstimatedLiability(officer, 1500m, SubmittedAt.AddHours(4)));
        AssertWorkflowError(
            ClaimWorkflowError.ClaimStatusNotAllowed,
            () => claim.Reject(officer, "Changed my mind.", SubmittedAt.AddHours(4)));
    }

    [Fact]
    public void Decisions_are_only_allowed_while_under_review()
    {
        var claim = TakenClaim();
        claim.RequestInformation(officer, "Please send photos.", SubmittedAt.AddHours(2));

        AssertWorkflowError(
            ClaimWorkflowError.ClaimStatusNotAllowed,
            () => claim.Reject(officer, "Not covered.", SubmittedAt.AddHours(3)));
    }

    [Fact]
    public void Rejecting_records_a_decision_without_a_settlement_amount()
    {
        var claim = TakenClaim();

        claim.Reject(officer, "The loss is outside the policy period.", SubmittedAt.AddHours(2));

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal(ClaimDecisionOutcome.Rejected, claim.Decision?.Outcome);
        Assert.Null(claim.Decision?.SettlementAmount);
        Assert.Equal(ClaimHistoryEventType.Rejected, claim.HistoryEntries.Last().EventType);
    }

    private static ClaimSubmission Submission(ClaimType type) =>
        new(
            type,
            "POL-123456",
            SubmittedAt.AddDays(-2),
            "Orchard Road, Singapore",
            "Rear-ended at a junction.",
            type == ClaimType.Motor ? "SBA1234A" : null,
            type == ClaimType.Property ? "12 Example Street, Kuala Lumpur" : null,
            "SGD");

    private Claim Submit(ClaimType type = ClaimType.Motor) =>
        Claim.Submit(Submission(type), claimant, ClaimReference.Generate(), SubmittedAt);

    private Claim TakenClaim()
    {
        var claim = Submit();
        claim.Take(officer, SubmittedAt.AddHours(1));
        return claim;
    }

    private static void AssertWorkflowError(ClaimWorkflowError expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ClaimWorkflowException>(action).Error);
}
