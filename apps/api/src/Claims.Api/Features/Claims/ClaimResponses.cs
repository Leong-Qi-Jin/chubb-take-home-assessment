using Claims.Api.Domain.Claims;
using Claims.Api.Features.Shared;

namespace Claims.Api.Features.Claims;

public sealed record ClaimantResponse(string FullName, string Email, string? Phone);

public sealed record IncidentResponse(DateTimeOffset OccurredAt, string Location, string Description);

public sealed record MotorDetailsResponse(string VehicleRegistration);

public sealed record PropertyDetailsResponse(string PropertyAddress);

public sealed record InformationRequestResponse(
    Guid Id,
    InformationRequestStatus Status,
    string RequestText,
    DateTimeOffset RequestedAt,
    string? ResponseText,
    DateTimeOffset? RespondedAt,
    string? CancellationReason,
    DateTimeOffset? CancelledAt);

public sealed record ClaimDecisionResponse(
    ClaimDecisionOutcome Outcome,
    MoneyResponse? SettlementAmount,
    string Rationale,
    OfficerResponse DecidedBy,
    DateTimeOffset DecidedAt);

/// <summary>
/// Claim details. For claimants, <see cref="EstimatedLiability"/> and officer IDs are always null.
/// </summary>
public sealed record ClaimDetailsResponse(
    string Reference,
    ClaimType Type,
    ClaimStatus Status,
    string PolicyNumber,
    ClaimantResponse Claimant,
    IncidentResponse Incident,
    MotorDetailsResponse? Motor,
    PropertyDetailsResponse? Property,
    OfficerResponse? AssignedOfficer,
    string Currency,
    MoneyResponse? EstimatedLiability,
    IReadOnlyList<InformationRequestResponse> InformationRequests,
    ClaimDecisionResponse? Decision,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Maps a claim loaded with <see cref="ClaimQueries.WithDetails"/>.
    /// </summary>
    public static ClaimDetailsResponse From(Claim claim, bool forClaimant) =>
        new(
            claim.Reference,
            claim.Type,
            claim.Status,
            claim.PolicyNumber,
            new ClaimantResponse(claim.Claimant.FullName, claim.Claimant.Email, claim.Claimant.Phone),
            new IncidentResponse(claim.IncidentAt, claim.IncidentLocation, claim.IncidentDescription),
            claim.MotorDetail is { } motor ? new MotorDetailsResponse(motor.VehicleRegistration) : null,
            claim.PropertyDetail is { } property ? new PropertyDetailsResponse(property.PropertyAddress) : null,
            claim.AssignedOfficer is { } officer ? OfficerResponse.For(officer.Id, officer.DisplayName, forClaimant) : null,
            claim.CurrencyCode,
            forClaimant ? null : MoneyResponse.From(claim.EstimatedLiability, claim.CurrencyCode),
            [
                .. claim.InformationRequests
                    .OrderBy(request => request.RequestedAt)
                    .Select(request => new InformationRequestResponse(
                        request.Id,
                        request.Status,
                        request.RequestText,
                        request.RequestedAt,
                        request.ResponseText,
                        request.RespondedAt,
                        request.CancellationReason,
                        request.CancelledAt)),
            ],
            claim.Decision is { } decision
                ? new ClaimDecisionResponse(
                    decision.Outcome,
                    MoneyResponse.From(decision.SettlementAmount, claim.CurrencyCode),
                    decision.Rationale,
                    OfficerResponse.For(decision.DecidedBy.Id, decision.DecidedBy.DisplayName, forClaimant),
                    decision.DecidedAt)
                : null,
            claim.SubmittedAt,
            claim.UpdatedAt);
}
