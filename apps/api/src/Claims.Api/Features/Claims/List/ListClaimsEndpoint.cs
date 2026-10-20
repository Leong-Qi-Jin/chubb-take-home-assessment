using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Shared;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Features.Claims.List;

// A class rather than a positional record: OpenAPI generation fails on validation attributes on [AsParameters] constructor parameters. Unset properties bind as their type's default, so paging values are nullable.
public sealed class ListClaimsQuery
{
    public const int DefaultPageSize = 20;

    [FromQuery(Name = "status")]
    public ClaimStatus[]? Status { get; init; }

    [FromQuery(Name = "type")]
    public ClaimType? Type { get; init; }

    /// <summary>
    /// A staff ID, or <c>unassigned</c>.
    /// </summary>
    [FromQuery(Name = "assignedTo")]
    public string? AssignedTo { get; init; }

    [FromQuery(Name = "claimantId")]
    public Guid? ClaimantId { get; init; }

    [FromQuery(Name = "sort")]
    [AllowedValues(null, "submittedAt", "updatedAt")]
    public string? Sort { get; init; }

    [FromQuery(Name = "order")]
    [AllowedValues(null, "asc", "desc")]
    public string? Order { get; init; }

    // The upper bound keeps the row offset within the range of an int at the largest page size.
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue / 100)]
    public int? Page { get; init; }

    [FromQuery(Name = "pageSize")]
    [AllowedValues(null, 5, 10, 20, 50, 100)]
    public int? PageSize { get; init; }
}

/// <summary>
/// A claim in a list. For claimants, <see cref="EstimatedLiability"/> and officer IDs are always null.
/// </summary>
public sealed record ClaimSummaryResponse(
    string Reference,
    ClaimType Type,
    ClaimStatus Status,
    Guid ClaimantId,
    string ClaimantName,
    OfficerResponse? AssignedOfficer,
    MoneyResponse? EstimatedLiability,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt);

public sealed record ClaimPageResponse(
    IReadOnlyList<ClaimSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public static class ListClaimsEndpoint
{
    private const string Unassigned = "unassigned";

    public static void Map(RouteGroupBuilder claims) =>
        claims.MapGet("/", HandleAsync)
            .WithName("ListClaims")
            .RequireAuthorization()
            .ProducesProblems(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

    private static async Task<Results<Ok<ClaimPageResponse>, ValidationProblem>> HandleAsync(
        [AsParameters] ListClaimsQuery query,
        ClaimsPrincipal user,
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var caller = user.GetCaller();
        var errors = new Dictionary<string, string[]>();
        Guid? assignedOfficerId = null;

        if (caller.IsClaimant && query.AssignedTo is not null)
        {
            errors["assignedTo"] = ["Claimants cannot filter by assigned officer."];
        }
        else if (query.AssignedTo is not null and not Unassigned)
        {
            if (Guid.TryParse(query.AssignedTo, out var officerId))
            {
                assignedOfficerId = officerId;
            }
            else
            {
                errors["assignedTo"] = [$"Use a staff ID or \"{Unassigned}\"."];
            }
        }

        // Query binding accepts numeric enum values, so reject any that are not defined.
        if (query.Status?.Any(status => !Enum.IsDefined(status)) == true)
        {
            errors["status"] = [$"Use one of: {string.Join(", ", Enum.GetNames<ClaimStatus>())}."];
        }

        if (query.Type is { } requestedType && !Enum.IsDefined(requestedType))
        {
            errors["type"] = [$"Use one of: {string.Join(", ", Enum.GetNames<ClaimType>())}."];
        }

        if (caller.IsClaimant && query.ClaimantId is not null)
        {
            errors["claimantId"] = ["Claimants cannot filter by claimant."];
        }

        if (errors.Count > 0)
        {
            return ApiProblems.ValidationFailed(errors);
        }

        var claims = dbContext.Claims.AsNoTracking().VisibleTo(caller);

        if (query.Status is { Length: > 0 } statuses)
        {
            claims = claims.Where(claim => statuses.Contains(claim.Status));
        }

        if (query.Type is { } type)
        {
            claims = claims.Where(claim => claim.Type == type);
        }

        if (query.AssignedTo == Unassigned)
        {
            claims = claims.Where(claim => claim.AssignedOfficerId == null);
        }
        else if (assignedOfficerId is { } officerId)
        {
            claims = claims.Where(claim => claim.AssignedOfficerId == officerId);
        }

        if (query.ClaimantId is { } claimantId)
        {
            claims = claims.Where(claim => claim.ClaimantId == claimantId);
        }

        // Officers work the oldest claims first; claimants want their latest claims first.
        var descending = query.Order is null ? caller.IsClaimant : query.Order == "desc";
        var ordered = (query.Sort, descending) switch
        {
            ("updatedAt", false) => claims.OrderBy(claim => claim.UpdatedAt),
            ("updatedAt", true) => claims.OrderByDescending(claim => claim.UpdatedAt),
            (_, false) => claims.OrderBy(claim => claim.SubmittedAt),
            (_, true) => claims.OrderByDescending(claim => claim.SubmittedAt),
        };

        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? ListClaimsQuery.DefaultPageSize;
        var totalCount = await claims.CountAsync(cancellationToken);
        var rows = await ordered
            .ThenBy(claim => claim.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(claim => new
            {
                claim.Reference,
                claim.Type,
                claim.Status,
                claim.ClaimantId,
                ClaimantName = claim.Claimant.FullName,
                claim.AssignedOfficerId,
                OfficerName = claim.AssignedOfficer == null ? null : claim.AssignedOfficer.DisplayName,
                claim.EstimatedLiability,
                claim.CurrencyCode,
                claim.SubmittedAt,
                claim.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new ClaimSummaryResponse(
                row.Reference,
                row.Type,
                row.Status,
                row.ClaimantId,
                row.ClaimantName,
                row is { AssignedOfficerId: { } id, OfficerName: { } name }
                    ? OfficerResponse.For(id, name, caller.IsClaimant)
                    : null,
                caller.IsClaimant ? null : MoneyResponse.From(row.EstimatedLiability, row.CurrencyCode),
                row.SubmittedAt,
                row.UpdatedAt))
            .ToList();

        return TypedResults.Ok(new ClaimPageResponse(items, page, pageSize, totalCount));
    }
}
