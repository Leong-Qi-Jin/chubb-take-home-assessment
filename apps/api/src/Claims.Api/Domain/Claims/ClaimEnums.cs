namespace Claims.Api.Domain.Claims;

public enum ClaimType
{
    Motor,
    Property,
}

public enum ClaimStatus
{
    Submitted,
    UnderReview,
    AwaitingInformation,
    Settled,
    Rejected,
}

public enum ClaimDecisionOutcome
{
    Settled,
    Rejected,
}

public enum InformationRequestStatus
{
    Open,
    Answered,
    Cancelled,
}

public enum ClaimHistoryEventType
{
    Submitted,
    OfficerAssigned,
    EstimatedLiabilityUpdated,
    InformationRequested,
    InformationProvided,
    InformationRequestCancelled,
    Settled,
    Rejected,
}

public enum ClaimHistoryActorType
{
    Claimant,
    ClaimsOfficer,
    System,
}
