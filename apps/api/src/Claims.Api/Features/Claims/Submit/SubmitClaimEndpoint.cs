using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Claims.GetDetails;
using Claims.Api.Features.Currencies;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Claim = Claims.Api.Domain.Claims.Claim;

namespace Claims.Api.Features.Claims.Submit;

public sealed record IncidentRequest(
    [property: Required] DateTimeOffset? OccurredAt,
    [property: Required, MaxLength(500)] string Location,
    [property: Required, MaxLength(2000)] string Description);

public sealed record MotorDetailsRequest([property: Required, MaxLength(50)] string VehicleRegistration);

public sealed record PropertyDetailsRequest([property: Required, MaxLength(500)] string PropertyAddress);

public sealed record SubmitClaimRequest(
    [property: Required] ClaimType? Type,
    [property: Required, MaxLength(50)] string PolicyNumber,
    [property: Required] IncidentRequest Incident,
    MotorDetailsRequest? Motor,
    PropertyDetailsRequest? Property,
    [property: Required] string Currency);

public static class SubmitClaimEndpoint
{
    public static void Map(RouteGroupBuilder claims) =>
        claims.MapPost("/", HandleAsync)
            .WithName("SubmitClaim")
            .RequireAuthorization(CallerRoles.Claimant)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden);

    private static async Task<Results<CreatedAtRoute<ClaimDetailsResponse>, ValidationProblem>> HandleAsync(
        SubmitClaimRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        ClaimsDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<CurrencyOptions> currencyOptions,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var errors = Validate(request, now, currencyOptions.Value);

        if (errors.Count > 0)
        {
            return ApiProblems.ValidationFailed(errors);
        }

        var caller = user.GetCaller();
        var claimant = await dbContext.Claimants.SingleAsync(claimant => claimant.Id == caller.Id, cancellationToken);
        var claim = Claim.Submit(
            new ClaimSubmission(
                request.Type!.Value,
                request.PolicyNumber,
                request.Incident.OccurredAt!.Value,
                request.Incident.Location,
                request.Incident.Description,
                request.Motor?.VehicleRegistration,
                request.Property?.PropertyAddress,
                request.Currency),
            claimant,
            ClaimReference.Generate(),
            now);

        dbContext.Claims.Add(claim);
        await dbContext.SaveChangesAsync(cancellationToken);

        ClaimETag.Write(httpContext.Response, claim.Version);
        return TypedResults.CreatedAtRoute(
            ClaimDetailsResponse.From(claim, forClaimant: true),
            GetClaimDetailsEndpoint.RouteName,
            new { reference = claim.Reference });
    }

    /// <summary>
    /// Checks the rules that attributes cannot express.
    /// </summary>
    private static Dictionary<string, string[]> Validate(
        SubmitClaimRequest request,
        DateTimeOffset now,
        CurrencyOptions currencyOptions)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Type == ClaimType.Motor && request.Motor is null)
        {
            errors["motor"] = ["Vehicle details are required for a motor claim."];
        }

        if (request.Type == ClaimType.Property && request.Property is null)
        {
            errors["property"] = ["Property details are required for a property claim."];
        }

        if (request.Type != ClaimType.Motor && request.Motor is not null)
        {
            errors["motor"] = ["Only motor claims can include vehicle details."];
        }

        if (request.Type != ClaimType.Property && request.Property is not null)
        {
            errors["property"] = ["Only property claims can include property details."];
        }

        if (request.Incident.OccurredAt > now)
        {
            errors["incident.occurredAt"] = ["The incident date cannot be in the future."];
        }

        if (!currencyOptions.Supported.Contains(request.Currency, StringComparer.Ordinal))
        {
            errors["currency"] = [$"Choose one of: {string.Join(", ", currencyOptions.Supported)}."];
        }

        return errors;
    }
}
