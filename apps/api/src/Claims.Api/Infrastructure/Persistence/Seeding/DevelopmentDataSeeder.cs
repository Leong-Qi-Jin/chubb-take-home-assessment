using Claims.Api.Domain.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Claims.Api.Infrastructure.Persistence.Seeding;

public static class DevelopmentDataSeeder
{
    private static readonly DateTimeOffset SeedNow = new(2026, 10, 20, 4, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(ClaimsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var claimants = await GetOrAddClaimantsAsync(dbContext, cancellationToken);
        var officers = await GetOrAddOfficersAsync(dbContext, cancellationToken);
        var claims = CreateClaims(claimants, officers).ToArray();
        var seededClaimIds = claims.Select(claim => claim.Id).ToArray();
        var existingClaimIds = await dbContext.Claims
            .Where(claim => seededClaimIds.Contains(claim.Id))
            .Select(claim => claim.Id)
            .ToHashSetAsync(cancellationToken);

        dbContext.Claims.AddRange(claims.Where(claim => !existingClaimIds.Contains(claim.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Claimant[]> GetOrAddClaimantsAsync(
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var claimants = CreateClaimants();
        var seededIds = claimants.Select(claimant => claimant.Id).ToArray();
        var existingClaimants = await dbContext.Claimants
            .Where(claimant => seededIds.Contains(claimant.Id))
            .ToDictionaryAsync(claimant => claimant.Id, cancellationToken);

        return claimants.Select(claimant =>
        {
            if (existingClaimants.TryGetValue(claimant.Id, out var existingClaimant))
            {
                return existingClaimant;
            }

            dbContext.Claimants.Add(claimant);
            return claimant;
        }).ToArray();
    }

    private static async Task<StaffMember[]> GetOrAddOfficersAsync(
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var officers = CreateOfficers();
        var seededIds = officers.Select(officer => officer.Id).ToArray();
        var existingOfficers = await dbContext.StaffMembers
            .Where(officer => seededIds.Contains(officer.Id))
            .ToDictionaryAsync(officer => officer.Id, cancellationToken);

        return officers.Select(officer =>
        {
            if (existingOfficers.TryGetValue(officer.Id, out var existingOfficer))
            {
                return existingOfficer;
            }

            dbContext.StaffMembers.Add(officer);
            return officer;
        }).ToArray();
    }

    internal static Claimant[] CreateClaimants() =>
    [
        new()
        {
            Id = SeedGuid(1),
            Email = "alex.tan@example.com",
            FullName = "Alex Tan",
            Phone = "+65 8123 4567",
            CreatedAt = SeedNow.AddMonths(-8),
        },
        new()
        {
            Id = SeedGuid(2),
            Email = "maya.lim@example.com",
            FullName = "Maya Lim",
            Phone = "+65 8234 5678",
            CreatedAt = SeedNow.AddMonths(-7),
        },
        new()
        {
            Id = SeedGuid(3),
            Email = "daniel.wong@example.com",
            FullName = "Daniel Wong",
            Phone = null,
            CreatedAt = SeedNow.AddMonths(-6),
        },
        new()
        {
            Id = SeedGuid(4),
            Email = "priya.nair@example.com",
            FullName = "Priya Nair",
            Phone = "+60 12-345 6789",
            CreatedAt = SeedNow.AddMonths(-5),
        },
        new()
        {
            Id = SeedGuid(5),
            Email = "james.lee@example.com",
            FullName = "James Lee",
            Phone = null,
            CreatedAt = SeedNow.AddMonths(-4),
        },
        new()
        {
            Id = SeedGuid(6),
            Email = "siti.rahman@example.com",
            FullName = "Siti Rahman",
            Phone = "+60 17-654 3210",
            CreatedAt = SeedNow.AddMonths(-3),
        },
    ];

    internal static StaffMember[] CreateOfficers() =>
    [
        new()
        {
            Id = SeedGuid(101),
            Email = "sam.lee@claims.example",
            DisplayName = "Sam Lee",
            IsActive = true,
        },
        new()
        {
            Id = SeedGuid(102),
            Email = "aisha.khan@claims.example",
            DisplayName = "Aisha Khan",
            IsActive = true,
        },
        new()
        {
            Id = SeedGuid(103),
            Email = "benjamin.tan@claims.example",
            DisplayName = "Benjamin Tan",
            IsActive = true,
        },
        new()
        {
            Id = SeedGuid(104),
            Email = "former.officer@claims.example",
            DisplayName = "Former Officer",
            IsActive = false,
        },
    ];

    internal static IEnumerable<Claim> CreateClaims(
        IReadOnlyList<Claimant> claimants,
        IReadOnlyList<StaffMember> officers)
    {
        var statuses = new[]
        {
            ClaimStatus.Submitted,
            ClaimStatus.Submitted,
            ClaimStatus.Submitted,
            ClaimStatus.Submitted,
            ClaimStatus.Submitted,
            ClaimStatus.Submitted,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.UnderReview,
            ClaimStatus.AwaitingInformation,
            ClaimStatus.AwaitingInformation,
            ClaimStatus.AwaitingInformation,
            ClaimStatus.AwaitingInformation,
            ClaimStatus.Settled,
            ClaimStatus.Settled,
            ClaimStatus.Settled,
            ClaimStatus.Settled,
            ClaimStatus.Settled,
            ClaimStatus.Rejected,
            ClaimStatus.Rejected,
            ClaimStatus.Rejected,
        };

        for (var index = 0; index < statuses.Length; index++)
        {
            var claimNumber = index + 1;
            var type = index % 2 == 0 ? ClaimType.Motor : ClaimType.Property;
            var claimant = HasPrimaryDemoClaimant(claimNumber)
                ? claimants[0]
                : claimants[(index % (claimants.Count - 1)) + 1];
            var officer = statuses[index] == ClaimStatus.Submitted ? null : officers[index % 3];
            decimal? estimatedLiability = statuses[index] == ClaimStatus.Submitted
                ? null
                : 1500m + (index * 725m);
            var submittedAt = SeedNow.AddDays(-(statuses.Length - index));

            yield return CreateClaim(
                claimNumber,
                type,
                statuses[index],
                claimant,
                officer,
                estimatedLiability,
                submittedAt);
        }
    }

    private static bool HasPrimaryDemoClaimant(int claimNumber) =>
        claimNumber is 1 or 7 or 8 or 14 or 18 or 23;

    private static Claim CreateClaim(
        int number,
        ClaimType type,
        ClaimStatus status,
        Claimant claimant,
        StaffMember? officer,
        decimal? estimatedLiability,
        DateTimeOffset submittedAt)
    {
        var claim = new Claim
        {
            Id = SeedGuid(1000 + number),
            Reference = $"CLM-DEMO-{number:0000}",
            ClaimantId = claimant.Id,
            Claimant = claimant,
            Type = type,
            Status = status,
            PolicyNumber = $"POL-{(number % 6) + 1:000000}",
            IncidentAt = submittedAt.AddDays(-1).AddHours(-number),
            IncidentLocation = type == ClaimType.Motor
                ? $"Junction {number}, Singapore"
                : $"{number} Example Street, Kuala Lumpur",
            IncidentDescription = type == ClaimType.Motor
                ? "Vehicle collision causing rear bumper damage."
                : "Storm damage affecting the insured property.",
            AssignedOfficerId = officer?.Id,
            AssignedOfficer = officer,
            EstimatedLiability = estimatedLiability,
            CurrencyCode = number % 3 == 0 ? "MYR" : "SGD",
            SubmittedAt = submittedAt,
            UpdatedAt = submittedAt,
        };

        if (type == ClaimType.Motor)
        {
            claim.MotorDetail = new MotorClaimDetail
            {
                ClaimId = claim.Id,
                Claim = claim,
                VehicleRegistration = $"SBA{number:0000}A",
            };
        }
        else
        {
            claim.PropertyDetail = new PropertyClaimDetail
            {
                ClaimId = claim.Id,
                Claim = claim,
                PropertyAddress = $"{number} Example Street, Kuala Lumpur",
            };
        }

        AddHistory(
            claim,
            number,
            1,
            ClaimHistoryEventType.Submitted,
            "Claim submitted.",
            ClaimHistoryActorType.Claimant,
            submittedAt,
            claimant: claimant);

        if (officer is null)
        {
            return claim;
        }

        var assignedAt = submittedAt.AddHours(4);
        AddHistory(
            claim,
            number,
            2,
            ClaimHistoryEventType.OfficerAssigned,
            $"Claim assigned to {officer.DisplayName}.",
            ClaimHistoryActorType.ClaimsOfficer,
            assignedAt,
            officer: officer);

        if (estimatedLiability is not null)
        {
            var estimatedAt = assignedAt.AddHours(2);
            claim.UpdatedAt = estimatedAt;
            AddHistory(
                claim,
                number,
                3,
                ClaimHistoryEventType.EstimatedLiabilityUpdated,
                "Estimated liability recorded.",
                ClaimHistoryActorType.ClaimsOfficer,
                estimatedAt,
                officer: officer,
                details: JsonSerializer.Serialize(new
                {
                    amount = estimatedLiability.Value,
                    currency = claim.CurrencyCode,
                }));
        }

        if (status == ClaimStatus.AwaitingInformation)
        {
            AddOpenInformationRequest(claim, number, officer);
        }
        else if (status == ClaimStatus.Settled || status == ClaimStatus.Rejected)
        {
            AddDecision(claim, number, officer, status);
        }
        else if (number is 8 or 10)
        {
            AddClosedInformationRequest(claim, number, officer, number == 8);
        }

        return claim;
    }

    private static void AddOpenInformationRequest(Claim claim, int number, StaffMember officer)
    {
        var requestedAt = claim.UpdatedAt.AddHours(2);
        var request = new InformationRequest
        {
            Id = SeedGuid(3000 + number),
            ClaimId = claim.Id,
            Claim = claim,
            RequestedById = officer.Id,
            RequestedBy = officer,
            RequestText = "Please provide the incident report number and any witness details.",
            RequestedAt = requestedAt,
            Status = InformationRequestStatus.Open,
        };
        claim.InformationRequests.Add(request);
        claim.UpdatedAt = requestedAt;
        AddHistory(
            claim,
            number,
            4,
            ClaimHistoryEventType.InformationRequested,
            "Additional information requested.",
            ClaimHistoryActorType.ClaimsOfficer,
            requestedAt,
            officer: officer,
            request: request);
    }

    private static void AddClosedInformationRequest(
        Claim claim,
        int number,
        StaffMember officer,
        bool answered)
    {
        var requestedAt = claim.UpdatedAt.AddHours(2);
        var closedAt = requestedAt.AddDays(1);
        var request = new InformationRequest
        {
            Id = SeedGuid(3000 + number),
            ClaimId = claim.Id,
            Claim = claim,
            RequestedById = officer.Id,
            RequestedBy = officer,
            RequestText = "Please clarify the sequence of events.",
            RequestedAt = requestedAt,
            Status = answered ? InformationRequestStatus.Answered : InformationRequestStatus.Cancelled,
            ResponseText = answered ? "The incident occurred while the vehicle was stationary." : null,
            RespondedAt = answered ? closedAt : null,
            CancelledById = answered ? null : officer.Id,
            CancelledBy = answered ? null : officer,
            CancelledAt = answered ? null : closedAt,
            CancellationReason = answered ? null : "The requested detail was found in the original report.",
        };
        claim.InformationRequests.Add(request);
        AddHistory(
            claim,
            number,
            4,
            ClaimHistoryEventType.InformationRequested,
            "Additional information requested.",
            ClaimHistoryActorType.ClaimsOfficer,
            requestedAt,
            officer: officer,
            request: request);
        AddHistory(
            claim,
            number,
            5,
            answered
                ? ClaimHistoryEventType.InformationProvided
                : ClaimHistoryEventType.InformationRequestCancelled,
            answered ? "Additional information provided." : "Information request cancelled.",
            answered ? ClaimHistoryActorType.Claimant : ClaimHistoryActorType.ClaimsOfficer,
            closedAt,
            claimant: answered ? claim.Claimant : null,
            officer: answered ? null : officer,
            request: request);
        claim.UpdatedAt = closedAt;
    }

    private static void AddDecision(
        Claim claim,
        int number,
        StaffMember officer,
        ClaimStatus status)
    {
        var decidedAt = claim.UpdatedAt.AddDays(2);
        var settled = status == ClaimStatus.Settled;
        claim.Decision = new ClaimDecision
        {
            ClaimId = claim.Id,
            Claim = claim,
            DecidedById = officer.Id,
            DecidedBy = officer,
            Outcome = settled ? ClaimDecisionOutcome.Settled : ClaimDecisionOutcome.Rejected,
            SettlementAmount = settled ? claim.EstimatedLiability!.Value * 0.9m : null,
            Rationale = settled
                ? "The documented loss is covered under the policy."
                : "The reported loss falls outside the policy coverage.",
            DecidedAt = decidedAt,
        };
        claim.UpdatedAt = decidedAt;
        AddHistory(
            claim,
            number,
            4,
            settled ? ClaimHistoryEventType.Settled : ClaimHistoryEventType.Rejected,
            settled ? "Claim settled." : "Claim rejected.",
            ClaimHistoryActorType.ClaimsOfficer,
            decidedAt,
            officer: officer);
    }

    private static void AddHistory(
        Claim claim,
        int claimNumber,
        int sequence,
        ClaimHistoryEventType eventType,
        string summary,
        ClaimHistoryActorType actorType,
        DateTimeOffset occurredAt,
        Claimant? claimant = null,
        StaffMember? officer = null,
        InformationRequest? request = null,
        string? details = null)
    {
        claim.HistoryEntries.Add(new ClaimHistoryEntry
        {
            Id = SeedGuid(10_000 + (claimNumber * 10) + sequence),
            ClaimId = claim.Id,
            Claim = claim,
            EventType = eventType,
            Summary = summary,
            ActorType = actorType,
            ActorClaimantId = claimant?.Id,
            ActorClaimant = claimant,
            ActorStaffMemberId = officer?.Id,
            ActorStaffMember = officer,
            InformationRequestId = request?.Id,
            InformationRequest = request,
            Details = details,
            OccurredAt = occurredAt,
        });
    }

    private static Guid SeedGuid(int value) =>
        Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
}
