using System.ComponentModel.DataAnnotations;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.Reject;

public sealed record RejectClaimRequest([property: Required, MaxLength(2000)] string Rationale);

public static class RejectClaimEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/reject",
                (string reference,
                    RejectClaimRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.Reject(context.RequireOfficer(), request.Rationale, context.Now),
                        cancellationToken))
            .WithName("RejectClaim")
            .RequireAuthorization(CallerRoles.Officer)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status428PreconditionRequired)
            .ProducesValidationProblem();
}
