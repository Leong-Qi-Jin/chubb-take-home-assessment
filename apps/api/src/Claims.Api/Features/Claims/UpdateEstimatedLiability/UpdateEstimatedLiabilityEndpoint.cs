using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Claims.Api.Infrastructure.Errors;
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
            .RequireAuthorization(CallerRoles.Officer)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status428PreconditionRequired)
            .ProducesValidationProblem();
}
