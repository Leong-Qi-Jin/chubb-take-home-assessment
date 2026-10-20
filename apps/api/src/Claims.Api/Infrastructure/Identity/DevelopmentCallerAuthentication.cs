using System.Security.Claims;
using System.Text.Encodings.Web;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Claims.Api.Infrastructure.Identity;

public static class CallerRoles
{
    public const string Claimant = "Claimant";
    public const string Officer = "Officer";
}

public sealed record Caller(Guid Id, string Role)
{
    public bool IsClaimant => Role == CallerRoles.Claimant;
    public bool IsOfficer => Role == CallerRoles.Officer;
}

public static class CallerPrincipalExtensions
{
    /// <summary>
    /// Reads the caller resolved by authentication. Only call this from endpoints that require authorization.
    /// </summary>
    public static Caller GetCaller(this ClaimsPrincipal user) =>
        new(
            Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("The caller is not authenticated.")),
            user.FindFirstValue(ClaimTypes.Role) ?? throw new InvalidOperationException("The caller has no role."));
}

public static class DevelopmentCallerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the development identity scheme and one authorization policy per role. A production identity provider can replace the scheme without changing the endpoints.
    /// </summary>
    public static IServiceCollection AddDevelopmentCallerIdentity(this IServiceCollection services)
    {
        services
            .AddAuthentication(DevelopmentCallerAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentCallerAuthenticationHandler>(
                DevelopmentCallerAuthenticationHandler.SchemeName,
                configureOptions: null);
        services.AddAuthorizationBuilder()
            .AddPolicy(CallerRoles.Claimant, policy => policy.RequireRole(CallerRoles.Claimant))
            .AddPolicy(CallerRoles.Officer, policy => policy.RequireRole(CallerRoles.Officer));

        return services;
    }
}

/// <summary>
/// Identifies a seeded claimant from <c>X-Claimant-Email</c> or an active staff member from <c>X-Staff-Id</c>. The headers are trusted without verification, so this scheme is for local development and demos only.
/// </summary>
public sealed class DevelopmentCallerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ClaimsDbContext dbContext,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "DevelopmentCaller";
    public const string ClaimantEmailHeader = "X-Claimant-Email";
    public const string StaffIdHeader = "X-Staff-Id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claimantEmail = Request.Headers[ClaimantEmailHeader].ToString();
        var staffId = Request.Headers[StaffIdHeader].ToString();

        if (claimantEmail.Length == 0 && staffId.Length == 0)
        {
            return AuthenticateResult.NoResult();
        }

        if (claimantEmail.Length > 0 && staffId.Length > 0)
        {
            return AuthenticateResult.Fail("Send only one identity header.");
        }

        if (claimantEmail.Length > 0)
        {
            var claimantId = await dbContext.Claimants
                .Where(claimant => claimant.Email == claimantEmail)
                .Select(claimant => (Guid?)claimant.Id)
                .SingleOrDefaultAsync(Context.RequestAborted);

            return claimantId is { } id
                ? Success(id, CallerRoles.Claimant)
                : AuthenticateResult.Fail("Unknown claimant.");
        }

        if (!Guid.TryParse(staffId, out var parsedStaffId))
        {
            return AuthenticateResult.Fail("Invalid staff ID.");
        }

        var isActiveOfficer = await dbContext.StaffMembers
            .AnyAsync(staffMember => staffMember.Id == parsedStaffId && staffMember.IsActive, Context.RequestAborted);

        return isActiveOfficer
            ? Success(parsedStaffId, CallerRoles.Officer)
            : AuthenticateResult.Fail("Unknown or inactive staff member.");
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(
            StatusCodes.Status401Unauthorized,
            ErrorCodes.CallerNotIdentified,
            $"Send a seeded claimant's email in {ClaimantEmailHeader} or an active staff ID in {StaffIdHeader}.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(
            StatusCodes.Status403Forbidden,
            ErrorCodes.RoleNotAllowed,
            "Your role cannot use this endpoint.");

    private AuthenticateResult Success(Guid id, string role)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)],
            SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    private async Task WriteProblemAsync(int status, string code, string detail)
    {
        Response.StatusCode = status;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = ApiProblems.CreateDetails(status, code, detail),
        });
    }
}
