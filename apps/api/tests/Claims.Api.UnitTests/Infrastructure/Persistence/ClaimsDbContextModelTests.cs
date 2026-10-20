using Claims.Api.Domain.Claims;
using Claims.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Claims.Api.UnitTests.Infrastructure.Persistence;

public sealed class ClaimsDbContextModelTests
{
    private readonly IModel model;

    public ClaimsDbContextModelTests()
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelTests;Integrated Security=True")
            .Options;
        using var dbContext = new ClaimsDbContext(options);
        model = dbContext.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void Maps_row_version_native_json_and_string_enums()
    {
        var claim = GetEntity<Claim>();
        var version = claim.FindProperty(nameof(Claim.Version));
        var status = claim.FindProperty(nameof(Claim.Status));
        var history = GetEntity<ClaimHistoryEntry>();
        var details = history.FindProperty(nameof(ClaimHistoryEntry.Details));

        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
        Assert.Equal("rowversion", version.GetColumnType());
        Assert.Equal(typeof(ClaimStatus), status?.ClrType);
        Assert.Equal("nvarchar(50)", status?.GetColumnType());
        Assert.Equal("json", details?.GetColumnType());
    }

    [Fact]
    public void Defines_workflow_constraints_and_query_indexes()
    {
        var claim = GetEntity<Claim>();
        var constraintNames = claim.GetCheckConstraints().Select(constraint => constraint.Name);

        Assert.Contains("CK_Claims_Assignment", constraintNames);
        Assert.Contains("CK_Claims_CurrencyCode", constraintNames);
        Assert.Contains("CK_Claims_SettledEstimate", constraintNames);
        Assert.Contains(
            claim.GetIndexes(),
            index => index.IsUnique && PropertyNames(index).SequenceEqual([nameof(Claim.Reference)]));
        Assert.Contains(
            claim.GetIndexes(),
            index => PropertyNames(index).SequenceEqual([nameof(Claim.Status), nameof(Claim.SubmittedAt)]));

        var informationRequest = GetEntity<InformationRequest>();
        Assert.Contains(
            informationRequest.GetIndexes(),
            index => index.IsUnique
                && index.GetFilter() == "[Status] = 'Open'"
                && PropertyNames(index).SequenceEqual([nameof(InformationRequest.ClaimId)]));
    }

    [Fact]
    public void Prevents_cascade_deletes_for_claim_records()
    {
        var foreignKeys = model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys());

        Assert.All(foreignKeys, foreignKey => Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior));
    }

    private IEntityType GetEntity<TEntity>() =>
        model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the EF model.");

    private static IEnumerable<string> PropertyNames(IReadOnlyIndex index) =>
        index.Properties.Select(property => property.Name);
}
