using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.Take;

public static class TakeClaimEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/take",
                (string reference, HttpContext httpContext, ClaimCommandRunner runner, CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.Take(context.RequireOfficer(), context.Now),
                        cancellationToken,
                        requireIfMatch: false))
            .WithName("TakeClaim")
            .RequireAuthorization(CallerRoles.Officer)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);
}
