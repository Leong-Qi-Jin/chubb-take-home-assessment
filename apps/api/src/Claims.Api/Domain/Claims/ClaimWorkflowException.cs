namespace Claims.Api.Domain.Claims;

public enum ClaimWorkflowError
{
    NotAssignedOfficer,
    ClaimAlreadyAssigned,
    ClaimStatusNotAllowed,
    InformationRequestNotFound,
    InformationRequestNotOpen,
    EstimatedLiabilityRequired,
}

/// <summary>
/// A workflow rule rejected an action. The API maps each error to a status code and a stable error code.
/// </summary>
public sealed class ClaimWorkflowException(ClaimWorkflowError error, string message) : Exception(message)
{
    public ClaimWorkflowError Error { get; } = error;
}
