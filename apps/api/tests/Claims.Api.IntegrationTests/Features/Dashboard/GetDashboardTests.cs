using System.Text.Json;
using Claims.Api.Domain.Claims;
using Claims.Api.Features.Dashboard;
using Claims.Api.IntegrationTests.Infrastructure;
using static Claims.Api.IntegrationTests.Infrastructure.ApiClient;

namespace Claims.Api.IntegrationTests.Features.Dashboard;

[Collection(ApiCollection.Name)]
public sealed class GetDashboardTests(ClaimsApiFactory factory)
{
    private readonly HttpClient alex = factory.CreateClaimantClient(TestUsers.Alex);
    private readonly HttpClient aisha = factory.CreateOfficerClient(TestUsers.Aisha);

    [Fact]
    public async Task Workload_and_outstanding_liability_reflect_open_claims_by_currency()
    {
        // Other tests share the database, so compare against a baseline rather than absolute totals.
        var before = await GetDashboardAsync();

        await TakeWithEstimateAsync("HKD", "1000.25");
        await TakeWithEstimateAsync("HKD", "500.00");
        var settled = await TakeWithEstimateAsync("HKD", "9999.00");
        await aisha.ApiPostAsync(
            $"/claims/{settled.Claim.Reference}/settle",
            new { settlementAmount = "9000.00", rationale = "Covered." },
            settled.ETag);
        await alex.SubmitClaimAsync(MotorClaim("HKD"));

        var after = await GetDashboardAsync();

        Assert.Equal(before.CountsByStatus[ClaimStatus.Submitted] + 1, after.CountsByStatus[ClaimStatus.Submitted]);
        Assert.Equal(before.CountsByStatus[ClaimStatus.UnderReview] + 2, after.CountsByStatus[ClaimStatus.UnderReview]);
        Assert.Equal(before.CountsByStatus[ClaimStatus.Settled] + 1, after.CountsByStatus[ClaimStatus.Settled]);
        Assert.Equal(before.MyOpenClaims + 2, after.MyOpenClaims);
        Assert.Equal(after.MyOpenClaims, after.OpenClaimsByOfficer.Single(row => row.Officer.Id == TestUsers.Aisha.Id).Count);
        Assert.DoesNotContain(after.OpenClaimsByOfficer, row => row.Officer.Id == TestUsers.FormerOfficer.Id);
        Assert.Equal(HkdTotal(before) + 1500.25m, HkdTotal(after));
    }

    private async Task<ApiResponse> TakeWithEstimateAsync(string currency, string amount)
    {
        var reference = (await alex.SubmitClaimAsync(MotorClaim(currency))).Claim.Reference;
        var taken = await aisha.ApiPostAsync($"/claims/{reference}/take");
        return await aisha.ApiPutAsync($"/claims/{reference}/estimated-liability", new { amount }, taken.ETag);
    }

    private async Task<DashboardResponse> GetDashboardAsync() =>
        (await aisha.ApiGetAsync("/dashboard")).Body.Deserialize<DashboardResponse>(Json)!;

    private static decimal HkdTotal(DashboardResponse dashboard) =>
        dashboard.OutstandingLiability
            .Where(total => total.Currency == "HKD")
            .Sum(total => decimal.Parse(total.Amount, System.Globalization.CultureInfo.InvariantCulture));
}
