using Claims.Api.Infrastructure.Errors;

namespace Claims.Api.Features.Claims;

internal static class ClaimCommandRoute
{
    /// <summary>
    /// Workflow actions share the same role and Problem Details contract. Taking a claim is the exception: it does not require If-Match.
    /// </summary>
    public static RouteHandlerBuilder WithClaimCommandContract(
        this RouteHandlerBuilder builder,
        string role,
        bool requireIfMatch = true)
    {
        builder
            .RequireAuthorization(role)
            .ProducesProblems(
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);

        if (requireIfMatch)
        {
            builder
                .ProducesProblems(StatusCodes.Status428PreconditionRequired)
                .ProducesValidationProblem();
        }

        return builder;
    }
}
