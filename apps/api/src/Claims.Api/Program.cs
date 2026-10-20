using System.Text.Json.Serialization;
using Claims.Api.Features.Claims;
using Claims.Api.Features.Currencies;
using Claims.Api.Features.Dashboard;
using Claims.Api.Infrastructure.Errors;
using Claims.Api.Infrastructure.Identity;
using Claims.Api.Infrastructure.Persistence;
using Claims.Api.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<ProblemSchemaTransformer>());
builder.Services.AddProblemDetails(ApiProblems.Configure);
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // The contract sends enums as names only; numbers would let undefined values reach the domain.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

    // The web defaults also read numbers from strings, which OpenAPI would publish as "integer or string".
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Return binding failures as 400 Problem Details instead of throwing, in every environment.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ClaimsDatabase"),
        sqlServerOptions => sqlServerOptions.UseCompatibilityLevel(170)));
builder.Services.AddDevelopmentCallerIdentity();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ClaimCommandRunner>();
builder.Services.AddOptions<CurrencyOptions>()
    .BindConfiguration(CurrencyOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");
api.MapClaimsEndpoints();
api.MapListCurrencies();
api.MapGetDashboard();

app.Run();
