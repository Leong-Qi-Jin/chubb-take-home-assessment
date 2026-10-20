using System.Net;
using System.Text.Json;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Claims.List;
using Claims.Api.IntegrationTests.Infrastructure;
using static Claims.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Claims.Api.IntegrationTests.Features.Claims;

[Collection(ApiCollection.Name)]
public sealed class ListClaimsTests(ClaimsApiFactory factory)
{
    private readonly HttpClient sam = factory.CreateOfficerClient(TestUsers.Sam);

    [Fact]
    public async Task Claimants_page_through_only_their_own_claims_newest_first()
    {
        var daniel = factory.CreateClaimantClient(TestUsers.Daniel);
        var submitted = new List<string>();

        for (var index = 0; index < 6; index++)
        {
            submitted.Add((await daniel.SubmitClaimAsync()).Claim.Reference);
        }

        await factory.CreateClaimantClient(TestUsers.Alex).SubmitClaimAsync();

        var firstPage = Page(await daniel.ApiGetAsync("/claims?pageSize=5"));
        var secondPage = Page(await daniel.ApiGetAsync("/claims?pageSize=5&page=2"));
        var allReferences = firstPage.Items.Concat(secondPage.Items).Select(item => item.Reference).ToList();

        Assert.Equal(firstPage.TotalCount, allReferences.Count);
        Assert.All(firstPage.Items.Concat(secondPage.Items), item => Assert.Equal(TestUsers.Daniel.Id, item.ClaimantId));
        Assert.Equal(submitted.AsEnumerable().Reverse().Take(5), firstPage.Items.Select(item => item.Reference));
        Assert.All(firstPage.Items, item => Assert.Null(item.EstimatedLiability));
    }

    [Fact]
    public async Task Officers_filter_the_queue_oldest_first()
    {
        var alex = factory.CreateClaimantClient(TestUsers.Alex);
        var older = (await alex.SubmitClaimAsync()).Claim.Reference;
        var newer = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = (await alex.SubmitClaimAsync()).Claim.Reference;
        await sam.ApiPostAsync($"/claims/{taken}/take");

        var unassigned = Page(await sam.ApiGetAsync("/claims?status=Submitted&assignedTo=unassigned&pageSize=100"));
        var mine = Page(await sam.ApiGetAsync($"/claims?assignedTo={TestUsers.Sam.Id}&status=UnderReview&pageSize=100"));

        var unassignedReferences = unassigned.Items.Select(item => item.Reference).ToList();
        Assert.True(unassignedReferences.IndexOf(older) < unassignedReferences.IndexOf(newer));
        Assert.DoesNotContain(taken, unassignedReferences);
        Assert.Contains(mine.Items, item => item.Reference == taken && item.AssignedOfficer?.Id == TestUsers.Sam.Id);
    }

    [Fact]
    public async Task Invalid_paging_and_claimant_only_restrictions_are_rejected()
    {
        var alex = factory.CreateClaimantClient(TestUsers.Alex);

        var badPageSize = await sam.ApiGetAsync("/claims?pageSize=7");
        var claimantFilters = await alex.ApiGetAsync($"/claims?assignedTo={TestUsers.Sam.Id}&claimantId={TestUsers.Maya.Id}");

        Assert.Equal((HttpStatusCode.BadRequest, "validation_failed"), (badPageSize.Status, badPageSize.ErrorCode));
        Assert.True(badPageSize.Body.GetProperty("errors").TryGetProperty("pageSize", out _));
        Assert.Equal(
            ["assignedTo", "claimantId"],
            claimantFilters.Body.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order());
    }

    private static ClaimPageResponse Page(ApiResponse response) =>
        response.Status == HttpStatusCode.OK
            ? response.Body.Deserialize<ClaimPageResponse>(Json)!
            : throw new InvalidOperationException($"Listing failed with {response.Status}: {response.Body}");
}
