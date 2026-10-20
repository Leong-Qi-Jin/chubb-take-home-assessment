using System.Security.Claims;
using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Features.Claims.GetHistory;

public sealed record ClaimHistoryEntryResponse(
    ClaimHistoryEventType EventType,
    string Summary,
    ClaimHistoryActorType ActorType,
    string ActorName,
    Guid? InformationRequestId,
    DateTimeOffset OccurredAt);

public static class GetClaimHistoryEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapGet("/{reference}/history", HandleAsync)
            .WithName("GetClaimHistory")
            .RequireAuthorization()
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<List<ClaimHistoryEntryResponse>>, ProblemHttpResult>> HandleAsync(
        string reference,
        ClaimsPrincipal user,
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var caller = user.GetCaller();
        var claimId = await dbContext.Claims
            .VisibleTo(caller)
            .Where(claim => claim.Reference == reference)
            .Select(claim => (Guid?)claim.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (claimId is null)
        {
            return ClaimQueries.ClaimNotFound(reference);
        }

        var entries = dbContext.ClaimHistoryEntries.Where(entry => entry.ClaimId == claimId);

        // Liability estimates are internal to claims staff.
        if (caller.IsClaimant)
        {
            entries = entries.Where(entry => entry.EventType != ClaimHistoryEventType.EstimatedLiabilityUpdated);
        }

        var history = await entries
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.Id)
            .Select(entry => new ClaimHistoryEntryResponse(
                entry.EventType,
                entry.Summary,
                entry.ActorType,
                entry.ActorClaimant != null
                    ? entry.ActorClaimant.FullName
                    : entry.ActorStaffMember != null ? entry.ActorStaffMember.DisplayName : "System",
                entry.InformationRequestId,
                entry.OccurredAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(history);
    }
}
