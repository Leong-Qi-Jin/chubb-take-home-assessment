using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace Claims.Api.Features.Claims;

public sealed record ClaimCommandContext(Claim Claim, StaffMember? Officer, DateTimeOffset Now)
{
    public StaffMember RequireOfficer() =>
        Officer ?? throw new InvalidOperationException("This command must be restricted to officers.");
}

/// <summary>
/// Runs a workflow action on one claim: finds it for the caller, checks <c>If-Match</c>, applies the action, and saves the claim and its history in one transaction.
/// </summary>
public sealed class ClaimCommandRunner(ClaimsDbContext dbContext, TimeProvider timeProvider)
{
    /// <param name="requireIfMatch">False only for taking a claim, where the only competing change is another officer taking it first, so a save conflict means <c>claim_already_assigned</c>.</param>
    public async Task<Results<Ok<ClaimDetailsResponse>, ProblemHttpResult>> ExecuteAsync(
        HttpContext httpContext,
        string reference,
        Action<ClaimCommandContext> command,
        CancellationToken cancellationToken,
        bool requireIfMatch = true)
    {
        var caller = httpContext.User.GetCaller();
        var claim = await dbContext.Claims
            .WithDetails()
            .VisibleTo(caller)
            .SingleOrDefaultAsync(claim => claim.Reference == reference, cancellationToken);

        if (claim is null)
        {
            return ClaimQueries.ClaimNotFound(reference);
        }

        if (requireIfMatch)
        {
            if (!httpContext.Request.Headers.ContainsKey(HeaderNames.IfMatch))
            {
                return ApiProblems.Create(
                    StatusCodes.Status428PreconditionRequired,
                    ErrorCodes.IfMatchRequired,
                    "Send the claim's latest ETag in the If-Match header.");
            }

            if (!ClaimETag.Matches(httpContext.Request.GetTypedHeaders().IfMatch, claim.Version))
            {
                return Conflict(httpContext, claim, caller, ErrorCodes.ClaimModified);
            }
        }

        var officer = caller.IsOfficer
            ? await dbContext.StaffMembers.SingleAsync(staffMember => staffMember.Id == caller.Id, cancellationToken)
            : null;

        try
        {
            command(new ClaimCommandContext(claim, officer, timeProvider.GetUtcNow()));
        }
        catch (ClaimWorkflowException exception)
        {
            return ApiProblems.FromWorkflowError(exception);
        }

        try
        {
            // EF Core checks the row version read above, so a change saved since then fails instead of being overwritten.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        // A racing request can also fail on a unique key first, such as the one-decision-per-claim key, because EF Core may write that row before the claim update.
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException || IsUniqueKeyViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            var current = await dbContext.Claims
                .AsNoTracking()
                .WithDetails()
                .SingleAsync(currentClaim => currentClaim.Id == claim.Id, cancellationToken);

            return Conflict(
                httpContext,
                current,
                caller,
                requireIfMatch ? ErrorCodes.ClaimModified : ErrorCodes.ClaimAlreadyAssigned);
        }

        ClaimETag.Write(httpContext.Response, claim.Version);

        return TypedResults.Ok(ClaimDetailsResponse.From(claim, caller.IsClaimant));
    }

    private static bool IsUniqueKeyViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    /// <summary>
    /// Returns the current claim and its ETag so the client can show the change and retry.
    /// </summary>
    private static ProblemHttpResult Conflict(HttpContext httpContext, Claim current, Caller caller, string code)
    {
        ClaimETag.Write(httpContext.Response, current.Version);
        var detail = code == ErrorCodes.ClaimAlreadyAssigned
            ? "Another officer took this claim first."
            : "The claim was changed by someone else. Review the latest version and try again.";

        return ApiProblems.Create(
            StatusCodes.Status409Conflict,
            code,
            detail,
            [new("claim", ClaimDetailsResponse.From(current, caller.IsClaimant))]);
    }
}
