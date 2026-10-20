using System.Globalization;
using System.Text.Json;
using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Persistence.Seeding;

namespace Claims.Api.UnitTests.Infrastructure.Persistence;

public sealed class DevelopmentDataSeederTests
{
    [Fact]
    public void Covers_demo_workflows_dashboards_and_claimant_pagination()
    {
        var claimants = DevelopmentDataSeeder.CreateClaimants();
        var officers = DevelopmentDataSeeder.CreateOfficers();
        var claims = DevelopmentDataSeeder.CreateClaims(claimants, officers).ToArray();
        var primaryClaimant = Assert.Single(claimants, claimant => claimant.Email == "alex.tan@example.com");

        Assert.Equal(25, claims.Length);
        Assert.Equal(6, claims.Count(claim => claim.ClaimantId == primaryClaimant.Id));
        Assert.Equal(6, claims.Count(claim => claim.Status == ClaimStatus.Submitted));
        Assert.Equal(7, claims.Count(claim => claim.Status == ClaimStatus.UnderReview));
        Assert.Equal(4, claims.Count(claim => claim.Status == ClaimStatus.AwaitingInformation));
        Assert.Equal(5, claims.Count(claim => claim.Status == ClaimStatus.Settled));
        Assert.Equal(3, claims.Count(claim => claim.Status == ClaimStatus.Rejected));
        Assert.Contains(claims, claim => claim.CurrencyCode == "SGD");
        Assert.Contains(claims, claim => claim.CurrencyCode == "MYR");

        Assert.All(claims, claim =>
        {
            Assert.Equal(claim.Type == ClaimType.Motor, claim.MotorDetail is not null);
            Assert.Equal(claim.Type == ClaimType.Property, claim.PropertyDetail is not null);
            Assert.NotEmpty(claim.HistoryEntries);
        });

        var requests = claims.SelectMany(claim => claim.InformationRequests).ToArray();
        Assert.Equal(4, requests.Count(request => request.Status == InformationRequestStatus.Open));
        Assert.Single(requests, request => request.Status == InformationRequestStatus.Answered);
        Assert.Single(requests, request => request.Status == InformationRequestStatus.Cancelled);
        Assert.Equal(5, claims.Count(claim => claim.Decision?.Outcome == ClaimDecisionOutcome.Settled));
        Assert.Equal(3, claims.Count(claim => claim.Decision?.Outcome == ClaimDecisionOutcome.Rejected));

        var activeOfficerIds = officers.Where(officer => officer.IsActive).Select(officer => officer.Id).ToHashSet();
        Assert.All(
            activeOfficerIds,
            officerId => Assert.Contains(
                claims,
                claim => claim.AssignedOfficerId == officerId && claim.Status is not ClaimStatus.Settled and not ClaimStatus.Rejected));
        Assert.DoesNotContain(claims, claim => claim.AssignedOfficerId == officers.Single(officer => !officer.IsActive).Id);
    }

    [Fact]
    public void Produces_valid_json_in_a_comma_decimal_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var claimants = DevelopmentDataSeeder.CreateClaimants();
            var officers = DevelopmentDataSeeder.CreateOfficers();
            var details = DevelopmentDataSeeder.CreateClaims(claimants, officers)
                .SelectMany(claim => claim.HistoryEntries)
                .Where(entry => entry.Details is not null)
                .Select(entry => entry.Details!);

            Assert.All(details, detail => JsonDocument.Parse(detail).Dispose());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
