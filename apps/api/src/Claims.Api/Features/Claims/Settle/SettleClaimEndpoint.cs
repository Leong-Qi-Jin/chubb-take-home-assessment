using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.Settle;

public sealed record SettleClaimRequest(
    [property: Required, MoneyAmount] string SettlementAmount,
    [property: Required, MaxLength(2000)] string Rationale);

public static class SettleClaimEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/settle",
                (string reference,
                    SettleClaimRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.Settle(
                            context.RequireOfficer(),
                            decimal.Parse(request.SettlementAmount, CultureInfo.InvariantCulture),
                            request.Rationale,
                            context.Now),
                        cancellationToken))
            .WithName("SettleClaim")
            .WithClaimCommandContract(CallerRoles.Officer);
}
