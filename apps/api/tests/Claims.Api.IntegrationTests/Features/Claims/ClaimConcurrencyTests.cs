using System.Net;
using System.Security.Claims;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Claims;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using SecurityClaim = System.Security.Claims.Claim;

namespace Claims.Api.IntegrationTests.Features.Claims;

[Collection(ApiCollection.Name)]
public sealed class ClaimConcurrencyTests(ClaimsApiFactory factory)
{
    private readonly HttpClient alex = factory.CreateClaimantClient(TestUsers.Alex);
    private readonly HttpClient sam = factory.CreateOfficerClient(TestUsers.Sam);

    [Fact]
    public async Task Changes_require_the_latest_etag()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");
        var estimated = await sam.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount = "800.00" }, taken.ETag);

        var withoutIfMatch = await sam.ApiPostAsync($"/claims/{reference}/reject", new { rationale = "Not covered." });
        var stale = await sam.ApiPostAsync($"/claims/{reference}/reject", new { rationale = "Not covered." }, taken.ETag);

        Assert.NotEqual(taken.ETag, estimated.ETag);
        Assert.Equal((HttpStatusCode.PreconditionRequired, "if_match_required"), (withoutIfMatch.Status, withoutIfMatch.ErrorCode));
        Assert.Equal((HttpStatusCode.Conflict, "claim_modified"), (stale.Status, stale.ErrorCode));
        Assert.Equal(estimated.ETag, stale.ETag);
        Assert.Equal("800.00", stale.Body.GetProperty("claim").GetProperty("estimatedLiability").GetProperty("amount").GetString());
        Assert.Equal(ClaimStatus.UnderReview, (await sam.ApiGetAsync($"/claims/{reference}")).Claim.Status);
    }

    [Fact]
    public async Task Only_one_of_two_officers_racing_to_take_a_claim_succeeds()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var aisha = factory.CreateOfficerClient(TestUsers.Aisha);

        var responses = await Task.WhenAll(
            sam.ApiPostAsync($"/claims/{reference}/take"),
            aisha.ApiPostAsync($"/claims/{reference}/take"));

        Assert.Single(responses, response => response.Status == HttpStatusCode.OK);
        var loser = Assert.Single(responses, response => response.Status == HttpStatusCode.Conflict);
        Assert.Equal("claim_already_assigned", loser.ErrorCode);
    }

    [Fact]
    public async Task A_decision_saved_between_loading_and_saving_returns_a_conflict()
    {
        var reference = (await alex.SubmitClaimAsync()).Claim.Reference;
        var taken = await sam.ApiPostAsync($"/claims/{reference}/take");
        var estimated = await sam.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount = "500.00" }, taken.ETag);
        var settle = new { settlementAmount = "450.00", rationale = "Covered." };

        await using var scope = factory.Services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<ClaimCommandRunner>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new SecurityClaim(ClaimTypes.NameIdentifier, TestUsers.Sam.Id.ToString()), new SecurityClaim(ClaimTypes.Role, CallerRoles.Officer)],
                "Test")),
        };
        httpContext.Request.Headers.IfMatch = estimated.ETag;

        // A double-submitted decision commits first. EF Core then inserts this request's decision before updating the claim, so it fails on the one-decision-per-claim key rather than the row version.
        var result = await runner.ExecuteAsync(
            httpContext,
            reference,
            context =>
            {
                var competing = sam.ApiPostAsync($"/claims/{reference}/settle", settle, estimated.ETag).GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, competing.Status);
                context.Claim.Settle(context.RequireOfficer(), 450m, "Covered.", context.Now);
            },
            CancellationToken.None);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("claim_modified", problem.ProblemDetails.Extensions["code"]);
    }
}
