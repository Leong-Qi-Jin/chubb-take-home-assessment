using Claims.Api.Infrastructure.Persistence;
using Claims.Api.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClaimsDatabase"),
        sqlServerOptions => sqlServerOptions.UseCompatibilityLevel(170)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseCompatibility.EnsureLevel170Async(dbContext);

    if (app.Configuration.GetValue("SeedData:Enabled", true))
    {
        await DevelopmentDataSeeder.SeedAsync(dbContext);
    }
}

app.UseHttpsRedirection();

app.Run();
