using System.ComponentModel.DataAnnotations;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;

namespace Claims.Api.Features.Claims.CancelInformationRequest;

public sealed record CancelInformationRequestRequest([property: Required, MaxLength(500)] string CancellationReason);

public static class CancelInformationRequestEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost(
                "/{reference}/information-requests/{requestId:guid}/cancel",
                (string reference,
                    Guid requestId,
                    CancelInformationRequestRequest request,
                    HttpContext httpContext,
                    ClaimCommandRunner runner,
                    CancellationToken cancellationToken) =>
                    runner.ExecuteAsync(
                        httpContext,
                        reference,
                        context => context.Claim.CancelInformationRequest(
                            context.RequireOfficer(),
                            requestId,
                            request.CancellationReason,
                            context.Now),
                        cancellationToken))
            .WithName("CancelInformationRequest")
            .RequireAuthorization(CallerRoles.Officer)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status428PreconditionRequired)
            .ProducesValidationProblem();
}
