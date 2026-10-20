using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Claims.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ClaimsApiFactory>
{
    public const string Name = "API";
}

/// <summary>
/// Hosts the API against a new SQL Server database that is migrated for the test run and dropped afterwards. Requires the local SQL Server and <c>ConnectionStrings__ClaimsDatabase</c> from <c>.env</c>.
/// </summary>
public sealed class ClaimsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string connectionString;

    public ClaimsApiFactory()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsDatabase")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__ClaimsDatabase, for example by loading .env, before running integration tests.");
        connectionString = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"Claims_IntegrationTests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    public HttpClient CreateClaimantClient(Claimant claimant) =>
        CreateCallerClient("X-Claimant-Email", claimant.Email);

    public HttpClient CreateOfficerClient(StaffMember officer) =>
        CreateCallerClient("X-Staff-Id", officer.Id.ToString());

    public HttpClient CreateCallerClient(string header, string value)
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Add(header, value);
        return client;
    }

    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await dbContext.Database.MigrateAsync();
        await DatabaseCompatibility.EnsureLevel170Async(dbContext);
        dbContext.Claimants.AddRange(TestUsers.Claimants);
        dbContext.StaffMembers.AddRange(TestUsers.Officers);
        await dbContext.SaveChangesAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Any environment other than Development skips startup migration and demo seeding.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ClaimsDatabase", connectionString);
    }
}
