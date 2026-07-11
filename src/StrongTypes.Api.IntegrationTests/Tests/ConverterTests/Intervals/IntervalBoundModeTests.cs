using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies the per-bound <c>IntervalBoundMode</c> behaviors of the two-column
/// shape on both providers: <c>Stored</c> round-trips per-value flags through
/// their own columns, the <c>AlwaysInclusive</c> default rejects exclusive
/// bounds on save, and <c>AlwaysExclusive</c> restores the fixed bound on read
/// without a flag column and rejects inclusive bounds on save.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalBoundModeTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<StoredBoundsIntervalEntity, FiniteInterval<int>, FiniteInterval<int>?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    [Theory, MemberData(nameof(Providers))]
    public async Task StoredBounds_RoundTripPerValueFlags(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var halfOpen = FiniteInterval.Create(1, 10, endInclusive: false);
        var openStart = FiniteInterval.Create(3, 7, startInclusive: false);
        var entity = StoredBoundsIntervalEntity.Create(halfOpen, openStart);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var reloaded = await db.Set<StoredBoundsIntervalEntity>().SingleAsync(e => e.Id == entity.Id, Ct);
        Assert.Equal(halfOpen, reloaded.Value);
        Assert.Equal(openStart, reloaded.NullableValue);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task DefaultMode_RejectsAnExclusiveBoundOnSave(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        db.Add(FiniteIntervalColumnsEntity.Create(FiniteInterval.Create(1, 10, endInclusive: false), null));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        Assert.Contains("IntervalBoundMode.Stored", exception.Message);
        db.ChangeTracker.Clear();
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task AlwaysExclusiveEnd_RestoresTheBoundOnRead_AndRejectsInclusiveSaves(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var window = FiniteInterval.Create(1, 10, endInclusive: false);
        var entity = ExclusiveEndIntervalEntity.Create(window, null);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var reloaded = await db.Set<ExclusiveEndIntervalEntity>().SingleAsync(e => e.Id == entity.Id, Ct);
        Assert.Equal(window, reloaded.Value);
        Assert.False(reloaded.Value.EndInclusive);

        db.Add(ExclusiveEndIntervalEntity.Create(FiniteInterval.Create(1, 10), null));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        Assert.Contains("AlwaysExclusive", exception.Message);
        db.ChangeTracker.Clear();
    }
}
