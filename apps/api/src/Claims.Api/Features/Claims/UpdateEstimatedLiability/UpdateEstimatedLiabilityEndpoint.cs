using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.UpdateEstimatedLiability;

public sealed record UpdateEstimatedLiabilityRequest([property: Required, MoneyAmount] string Amount);

public static class UpdateEstimatedLiabilityEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPut(
                "/{reference}/estimated-liability",
                (string reference,
                    UpdateEstimatedLiabilityRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.UpdateEstimatedLiability(
                            context.RequireOfficer(),
                            decimal.Parse(request.Amount, CultureInfo.InvariantCulture),
                            context.Now),
                        cancellationToken))
            .WithName("UpdateEstimatedLiability")
            .WithClaimCommandContract(CallerRoles.Officer);
}
