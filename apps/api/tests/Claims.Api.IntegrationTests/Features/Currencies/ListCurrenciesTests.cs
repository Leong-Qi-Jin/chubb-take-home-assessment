using System.Net;
using Claims.Api.IntegrationTests.Infrastructure;

namespace Claims.Api.IntegrationTests.Features.Currencies;

[Collection(ApiCollection.Name)]
public sealed class ListCurrenciesTests(ClaimsApiFactory factory)
{
    [Fact]
    public async Task Lists_the_currencies_a_claim_can_be_submitted_in()
    {
        var alex = factory.CreateClaimantClient(TestUsers.Alex);

        var response = await alex.ApiGetAsync("/currencies");
        var currencies = response.Body.EnumerateArray().Select(currency => currency.GetString()!).ToList();

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotEmpty(currencies);
        Assert.Equal(HttpStatusCode.Created, (await alex.ApiPostAsync("/claims", ApiClient.MotorClaim(currencies[^1]))).Status);
    }
}
