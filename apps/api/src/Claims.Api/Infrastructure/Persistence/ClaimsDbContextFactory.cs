using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Claims.Api.Infrastructure.Persistence;

public sealed class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsDatabase")
            ?? "Server=localhost;Database=Claims;Integrated Security=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.UseCompatibilityLevel(170))
            .Options;

        return new ClaimsDbContext(options);
    }
}
