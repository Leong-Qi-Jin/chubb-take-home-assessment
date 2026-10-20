using System.Net;
using System.Text.Json;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Claims.GetHistory;
using Claims.Api.IntegrationTests.Infrastructure;
using static Claims.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Claims.Api.IntegrationTests.Features.Claims;

[Collection(ApiCollection.Name)]
public sealed class ClaimJourneyTests(ClaimsApiFactory factory)
{
    private readonly HttpClient alex = factory.CreateClaimantClient(TestUsers.Alex);
    private readonly HttpClient sam = factory.CreateOfficerClient(TestUsers.Sam);

    [Fact]
    public async Task Claim_moves_from_submission_through_information_request_to_settlement()
    {
        var submitted = await alex.SubmitClaimAsync();
        var reference = submitted.Claim.Reference;
        Assert.Equal(ClaimStatus.Submitted, submitted.Claim.Status);
        Assert.Matches("^CLM-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", reference);
        Assert.NotNull(submitted.ETag);

        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");
        Assert.Equal(HttpStatusCode.OK, taken.Status);
        Assert.Equal(TestUsers.Sam.Id, taken.Claim.AssignedOfficer?.Id);

        var estimated = await sam.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount = "4200.50" }, taken.ETag);
        Assert.Equal("4200.50", estimated.Claim.EstimatedLiability?.Amount);
        Assert.Equal("SGD", estimated.Claim.EstimatedLiability?.Currency);

        var requested = await sam.ApiPostAsync(
            $"/claims/{reference}/information-requests",
            new { requestText = "Please provide the police report number." },
            estimated.ETag);
        Assert.Equal(HttpStatusCode.Created, requested.Status);
        Assert.Equal(ClaimStatus.AwaitingInformation, requested.Claim.Status);
        var requestId = Assert.Single(requested.Claim.InformationRequests).Id;

        var claimantView = await alex.ApiGetAsync($"/claims/{reference}");
        var answered = await alex.ApiPostAsync(
            $"/claims/{reference}/information-requests/{requestId}/response",
            new { responseText = "SPF-2026-1234" },
            claimantView.ETag);
        Assert.Equal(ClaimStatus.UnderReview, answered.Claim.Status);
        Assert.Equal(InformationRequestStatus.Answered, Assert.Single(answered.Claim.InformationRequests).Status);

        var officerView = await sam.ApiGetAsync($"/claims/{reference}");
        var settled = await sam.ApiPostAsync(
            $"/claims/{reference}/settle",
            new { settlementAmount = "3900.00", rationale = "Covered under the policy." },
            officerView.ETag);
        Assert.Equal(HttpStatusCode.OK, settled.Status);
        Assert.Equal(ClaimStatus.Settled, settled.Claim.Status);
        Assert.Equal("3900.00", settled.Claim.Decision?.SettlementAmount?.Amount);
        Assert.Equal(TestUsers.Sam.Id, settled.Claim.Decision?.DecidedBy.Id);

        var history = (await sam.ApiGetAsync($"/claims/{reference}/history")).Body.Deserialize<List<ClaimHistoryEntryResponse>>(Json)!;
        Assert.Equal(
            [
                ClaimHistoryEventType.Submitted,
                ClaimHistoryEventType.OfficerAssigned,
                ClaimHistoryEventType.EstimatedLiabilityUpdated,
                ClaimHistoryEventType.InformationRequested,
                ClaimHistoryEventType.InformationProvided,
                ClaimHistoryEventType.Settled,
            ],
            history.Select(entry => entry.EventType));
        Assert.Equal(TestUsers.Alex.FullName, history[4].ActorName);
        Assert.Equal(requestId, history[4].InformationRequestId);
    }

    [Fact]
    public async Task Claimants_see_their_claim_without_internal_staff_details()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");
        await sam.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount = "1500.00" }, taken.ETag);

        var details = (await alex.ApiGetAsync($"/claims/{reference}")).Claim;
        var history = (await alex.ApiGetAsync($"/claims/{reference}/history")).Body.Deserialize<List<ClaimHistoryEntryResponse>>(Json)!;

        Assert.Null(details.EstimatedLiability);
        Assert.Null(details.AssignedOfficer?.Id);
        Assert.Equal(TestUsers.Sam.DisplayName, details.AssignedOfficer?.DisplayName);
        Assert.DoesNotContain(history, entry => entry.EventType == ClaimHistoryEventType.EstimatedLiabilityUpdated);
    }

    [Fact]
    public async Task Rejecting_a_claim_records_the_decision_and_makes_it_final()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");

        var rejected = await sam.ApiPostAsync($"/claims/{reference}/reject", new { rationale = "Outside the policy period." }, taken.ETag);
        var afterDecision = await sam.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount = "10.00" }, rejected.ETag);

        Assert.Equal(ClaimStatus.Rejected, rejected.Claim.Status);
        Assert.Null(rejected.Claim.Decision?.SettlementAmount);
        Assert.Equal(HttpStatusCode.Conflict, afterDecision.Status);
        Assert.Equal("claim_status_not_allowed", afterDecision.ErrorCode);
    }
}
