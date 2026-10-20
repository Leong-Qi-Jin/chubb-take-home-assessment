using System.ComponentModel.DataAnnotations;
using Claims.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Claims.Api.Features.Claims.RequestInformation;

public sealed record RequestInformationRequest([property: Required, MaxLength(2000)] string RequestText);

public static class RequestInformationEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/information-requests",
                async Task<Results<Created<ClaimDetailsResponse>, ProblemHttpResult>> (
                    string reference,
                    RequestInformationRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                {
                    var result = await runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.RequestInformation(context.RequireOfficer(), request.RequestText, context.Now),
                        cancellationToken);

                    // A new information request is a created resource; the contract returns the updated claim.
                    return result.Result switch
                    {
                        Ok<ClaimDetailsResponse> ok => TypedResults.Created((string?)null, ok.Value),
                        ProblemHttpResult problem => problem,
                        _ => throw new InvalidOperationException("Unexpected command result."),
                    };
                })
            .WithName("RequestInformation")
            .WithClaimCommandContract(CallerRoles.Officer);
}
