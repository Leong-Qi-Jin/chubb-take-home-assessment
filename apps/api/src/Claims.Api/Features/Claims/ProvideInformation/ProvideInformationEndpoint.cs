using System.ComponentModel.DataAnnotations;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.ProvideInformation;

public sealed record ProvideInformationRequest([property: Required, MaxLength(2000)] string ResponseText);

public static class ProvideInformationEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/information-requests/{requestId:guid}/response",
                (string reference,
                    Guid requestId,
                    ProvideInformationRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.ProvideInformation(requestId, request.ResponseText, context.Now),
                        cancellationToken))
            .WithName("ProvideInformation")
            .WithClaimCommandContract(CallerRoles.Claimant);
}
