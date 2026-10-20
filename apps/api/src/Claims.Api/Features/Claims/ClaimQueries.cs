using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Features.Claims;

public static class ClaimQueries
{
    /// <summary>
    /// Loads everything <see cref="ClaimDetailsResponse"/> and the workflow methods need.
    /// </summary>
    public static IQueryable<Claim> WithDetails(this IQueryable<Claim> claims) =>
        claims
            .Include(claim => claim.Claimant)
            .Include(claim => claim.AssignedOfficer)
            .Include(claim => claim.MotorDetail)
            .Include(claim => claim.PropertyDetail)
            .Include(claim => claim.InformationRequests)
            .Include(claim => claim.Decision!)
            .ThenInclude(decision => decision.DecidedBy);

    /// <summary>
    /// Officers can see every claim; claimants can see only their own.
    /// </summary>
    public static IQueryable<Claim> VisibleTo(this IQueryable<Claim> claims, Caller caller) =>
        caller.IsOfficer ? claims : claims.Where(claim => claim.ClaimantId == caller.Id);

    /// <summary>
    /// Claimants get the same result for another claimant's claim, so references cannot be probed.
    /// </summary>
    public static ProblemHttpResult ClaimNotFound(string reference) =>
        ApiProblems.Create(
            StatusCodes.Status404NotFound,
            ErrorCodes.ClaimNotFound,
            $"Claim {reference} was not found.");
}
