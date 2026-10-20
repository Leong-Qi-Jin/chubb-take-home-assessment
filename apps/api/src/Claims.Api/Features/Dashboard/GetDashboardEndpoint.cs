using System.Linq.Expressions;
using System.Security.Claims;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Shared;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Claim = Claims.Api.Domain.Claims.Claim;

namespace Claims.Api.Features.Dashboard;

public sealed record OfficerWorkloadResponse(OfficerResponse Officer, int Count);

public sealed record DashboardResponse(
    IReadOnlyDictionary<ClaimStatus, int> CountsByStatus,
    int MyOpenClaims,
    IReadOnlyList<OfficerWorkloadResponse> OpenClaimsByOfficer,
    IReadOnlyList<MoneyResponse> OutstandingLiability);

public static class GetDashboardEndpoint
{
    // An open claim is assigned and still being worked on.
    private static readonly Expression<Func<Claim, bool>> IsOpen = claim =>
        claim.Status == ClaimStatus.UnderReview || claim.Status == ClaimStatus.AwaitingInformation;

    public static IEndpointRouteBuilder MapGetDashboard(this IEndpointRouteBuilder api)
    {
        api.MapGet("/dashboard", HandleAsync)
            .WithTags("Dashboard")
            .WithName("GetDashboard")
            .RequireAuthorization(CallerRoles.Officer)
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden);

        return api;
    }

    private static async Task<Ok<DashboardResponse>> HandleAsync(
        ClaimsPrincipal user,
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var caller = user.GetCaller();
        var claims = dbContext.Claims.AsNoTracking();

        var statusCounts = await claims
            .GroupBy(claim => claim.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        // Include active officers with no open claims, and inactive officers who still have some, so no workload is hidden.
        var workload = await dbContext.StaffMembers
            .AsNoTracking()
            .Select(officer => new
            {
                officer.Id,
                officer.DisplayName,
                officer.IsActive,
                Count = officer.AssignedClaims.AsQueryable().Count(IsOpen),
            })
            .Where(officer => officer.IsActive || officer.Count > 0)
            .OrderBy(officer => officer.DisplayName)
            .ToListAsync(cancellationToken);

        // Every non-terminal claim counts towards exposure; a claim without an estimate contributes zero.
        var outstandingLiability = await claims
            .Where(claim => claim.Status != ClaimStatus.Settled && claim.Status != ClaimStatus.Rejected)
            .Where(claim => claim.EstimatedLiability != null)
            .GroupBy(claim => claim.CurrencyCode)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(claim => claim.EstimatedLiability!.Value) })
            .OrderBy(total => total.Currency)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new DashboardResponse(
            Enum.GetValues<ClaimStatus>().ToDictionary(status => status, status => statusCounts.GetValueOrDefault(status)),
            workload.SingleOrDefault(officer => officer.Id == caller.Id)?.Count ?? 0,
            [
                .. workload.Select(officer => new OfficerWorkloadResponse(
                    new OfficerResponse(officer.Id, officer.DisplayName),
                    officer.Count)),
            ],
            [.. outstandingLiability.Select(total => MoneyResponse.From(total.Amount, total.Currency))]));
    }
}
