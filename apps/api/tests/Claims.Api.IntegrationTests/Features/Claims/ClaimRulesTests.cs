using System.Net;
using Claims.Api.Domain.Claims;
using Claims.Api.IntegrationTests.Infrastructure;

namespace Claims.Api.IntegrationTests.Features.Claims;

/// <summary>
/// Checks that each kind of rejected request returns its documented status and error code.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ClaimRulesTests(ClaimsApiFactory factory)
{
    private readonly HttpClient alex = factory.CreateClaimantClient(TestUsers.Alex);
    private readonly HttpClient maya = factory.CreateClaimantClient(TestUsers.Maya);
    private readonly HttpClient sam = factory.CreateOfficerClient(TestUsers.Sam);
    private readonly HttpClient aisha = factory.CreateOfficerClient(TestUsers.Aisha);

    [Fact]
    public async Task Unidentified_callers_and_wrong_roles_are_refused()
    {
        var anonymous = await factory.CreateAnonymousClient().ApiGetAsync("/claims");
        var unknownClaimant = await factory.CreateCallerClient("X-Claimant-Email", "nobody@example.com").ApiGetAsync("/claims");
        var inactiveOfficer = await factory.CreateOfficerClient(TestUsers.FormerOfficer).ApiGetAsync("/dashboard");
        var claimantOnStaffEndpoint = await alex.ApiGetAsync("/dashboard");
        var officerSubmitting = await sam.ApiPostAsync("/claims", ApiClient.MotorClaim());

        Assert.All(
            [anonymous, unknownClaimant, inactiveOfficer],
            response => Assert.Equal((HttpStatusCode.Unauthorized, "caller_not_identified"), (response.Status, response.ErrorCode)));
        Assert.All(
            [claimantOnStaffEndpoint, officerSubmitting],
            response => Assert.Equal((HttpStatusCode.Forbidden, "role_not_allowed"), (response.Status, response.ErrorCode)));
    }

    [Fact]
    public async Task Claimants_cannot_see_or_change_other_claimants_claims()
    {
        var submitted = await alex.SubmitClaimAsync();
        var reference = submitted.Claim.Reference;

        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");
        var requested = await sam.ApiPostAsync($"/claims/{reference}/information-requests", new { requestText = "Photos, please." }, taken.ETag);
        var responsePath = $"/claims/{reference}/information-requests/{requested.Claim.InformationRequests[0].Id}/response";

        var details = await maya.ApiGetAsync($"/claims/{reference}");
        var history = await maya.ApiGetAsync($"/claims/{reference}/history");
        var otherClaimantAnswer = await maya.ApiPostAsync(responsePath, new { responseText = "Attached." }, requested.ETag);
        var officerAnswer = await sam.ApiPostAsync(responsePath, new { responseText = "Attached." }, requested.ETag);

        Assert.All(
            [details, history, otherClaimantAnswer],
            response => Assert.Equal((HttpStatusCode.NotFound, "claim_not_found"), (response.Status, response.ErrorCode)));
        Assert.Equal((HttpStatusCode.Forbidden, "role_not_allowed"), (officerAnswer.Status, officerAnswer.ErrorCode));
    }

    [Fact]
    public async Task Only_the_assigned_officer_can_change_a_taken_claim()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");

        var secondTake = await aisha.ApiPostAsync($"/claims/{reference}/take");
        var otherOfficerReject = await aisha.ApiPostAsync($"/claims/{reference}/reject", new { rationale = "Not covered." }, taken.ETag);

        Assert.Equal((HttpStatusCode.Conflict, "claim_already_assigned"), (secondTake.Status, secondTake.ErrorCode));
        Assert.Equal((HttpStatusCode.Forbidden, "not_assigned_officer"), (otherOfficerReject.Status, otherOfficerReject.ErrorCode));
    }

    [Fact]
    public async Task Workflow_rules_return_specific_conflict_codes()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");

        var settleWithoutEstimate = await sam.ApiPostAsync(
            $"/claims/{reference}/settle",
            new { settlementAmount = "100.00", rationale = "Covered." },
            taken.ETag);
        var requested = await sam.ApiPostAsync($"/claims/{reference}/information-requests", new { requestText = "Photos, please." }, taken.ETag);
        var secondRequest = await sam.ApiPostAsync($"/claims/{reference}/information-requests", new { requestText = "And receipts." }, requested.ETag);
        var requestId = Assert.Single(requested.Claim.InformationRequests).Id;
        var cancelled = await sam.ApiPostAsync(
            $"/claims/{reference}/information-requests/{requestId}/cancel",
            new { cancellationReason = "Found in the original report." },
            requested.ETag);
        var lateAnswer = await alex.ApiPostAsync(
            $"/claims/{reference}/information-requests/{requestId}/response",
            new { responseText = "Attached." },
            cancelled.ETag);
        var unknownRequest = await alex.ApiPostAsync(
            $"/claims/{reference}/information-requests/{Guid.NewGuid()}/response",
            new { responseText = "Attached." },
            cancelled.ETag);

        Assert.Equal((HttpStatusCode.Conflict, "estimated_liability_required"), (settleWithoutEstimate.Status, settleWithoutEstimate.ErrorCode));
        Assert.Equal((HttpStatusCode.Conflict, "claim_status_not_allowed"), (secondRequest.Status, secondRequest.ErrorCode));
        Assert.Equal(ClaimStatus.UnderReview, cancelled.Claim.Status);
        Assert.Equal((HttpStatusCode.Conflict, "information_request_not_open"), (lateAnswer.Status, lateAnswer.ErrorCode));
        Assert.Equal((HttpStatusCode.NotFound, "information_request_not_found"), (unknownRequest.Status, unknownRequest.ErrorCode));
    }

    [Fact]
    public async Task Invalid_input_returns_field_errors()
    {
        var missingDetails = await alex.ApiPostAsync("/claims", new
        {
            type = "Property",
            policyNumber = "POL-123456",
            incident = new { occurredAt = DateTimeOffset.UtcNow.AddDays(1), location = "Jalan Ampang", description = "Flooded kitchen." },
            motor = (object?)null,
            property = (object?)null,
            currency = "USD",
        });
        var missingText = await alex.ApiPostAsync("/claims", new
        {
            type = "Motor",
            policyNumber = "",
            incident = new { occurredAt = DateTimeOffset.UtcNow.AddDays(-1), location = "Orchard Road", description = "Dented door." },
            motor = new { vehicleRegistration = "SBA1234A" },
            currency = "SGD",
        });
        var taken = await sam.ApiPostAsync($"/claims/{(await alex.SubmitClaimAsync()).Claim.Reference}/take");
        var badAmount = await sam.ApiPutAsync($"/claims/{taken.Claim.Reference}/estimated-liability", new { amount = "-12.345" }, taken.ETag);

        Assert.Equal((HttpStatusCode.BadRequest, "validation_failed"), (missingDetails.Status, missingDetails.ErrorCode));
        Assert.Equal(
            ["currency", "incident.occurredAt", "property"],
            missingDetails.Body.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order());
        Assert.True(missingText.Body.GetProperty("errors").TryGetProperty("policyNumber", out _));
        Assert.True(badAmount.Body.GetProperty("errors").TryGetProperty("amount", out _));
    }
}
