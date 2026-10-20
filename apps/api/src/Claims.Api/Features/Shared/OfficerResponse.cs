namespace Claims.Api.Features.Shared;

/// <summary>
/// A staff member as shown to callers. <see cref="Id"/> is null for claimants, who never see officer IDs.
/// </summary>
public sealed record OfficerResponse(Guid? Id, string DisplayName)
{
    public static OfficerResponse For(Guid id, string displayName, bool forClaimant) =>
        new(forClaimant ? null : id, displayName);
}
