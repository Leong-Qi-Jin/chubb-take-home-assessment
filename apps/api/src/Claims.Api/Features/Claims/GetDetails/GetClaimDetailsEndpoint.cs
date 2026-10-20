using System.Security.Claims;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Features.Claims.GetDetails;

public static class GetClaimDetailsEndpoint
{
    public const string RouteName = "GetClaimDetails";

    public static void Map(RouteGroupBuilder claims) =>
        claims.MapGet("/{reference}", HandleAsync)
            .WithName(RouteName)
            .RequireAuthorization()
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<ClaimDetailsResponse>, ProblemHttpResult>> HandleAsync(
        string reference,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var caller = user.GetCaller();
        var claim = await dbContext.Claims
            .AsNoTracking()
            .WithDetails()
            .VisibleTo(caller)
            .SingleOrDefaultAsync(claim => claim.Reference == reference, cancellationToken);

        if (claim is null)
        {
            return ClaimQueries.ClaimNotFound(reference);
        }

        ClaimETag.Write(httpContext.Response, claim.Version);
        return TypedResults.Ok(ClaimDetailsResponse.From(claim, caller.IsClaimant));
    }
}
