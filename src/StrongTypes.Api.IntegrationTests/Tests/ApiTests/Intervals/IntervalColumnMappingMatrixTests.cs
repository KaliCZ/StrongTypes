using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Fills out the two-column persistence matrix for the explicit
/// <c>HasIntervalColumns</c> entry points not already exercised elsewhere: the
/// no-argument default, both bounds <see cref="IntervalBoundMode.AlwaysInclusive"/>,
/// and both bounds <see cref="IntervalBoundMode.AlwaysExclusive"/> — each for a
/// non-nullable and a nullable property. The remaining configurations are covered
/// alongside: the convention two-column default by <see cref="IntervalColumnEntityTests"/>,
/// the single JSON column by <see cref="IntervalEntityTests{TEntity, TInterval}"/>,
/// and per-value <see cref="IntervalBoundMode.Stored"/> bounds by
/// <see cref="IntervalBoundModeTests"/>. Each config round-trips through the real
/// endpoint columns on both providers, filters and orders by an endpoint, and
/// asserts the interval maps to two columns (no flag columns, since none is Stored).
/// </summary>
public abstract class IntervalColumnMappingMatrixTestsBase<TEntity>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, FiniteInterval<int>, FiniteInterval<int>?>(factory)
    where TEntity : class, IEntity<TEntity, FiniteInterval<int>, FiniteInterval<int>?>
{
    /// <summary>Two values valid under this config's bound mode, ordered so <c>First.Start &lt; Second.Start</c>.</summary>
    protected abstract FiniteInterval<int> First { get; }
    protected abstract FiniteInterval<int> Second { get; }

    [Fact]
    public async Task NonNullableValue_RoundTripsThroughColumns()
    {
        var entity = TEntity.Create(First, null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        await AssertEntity(entity.Id, First, null);
    }

    [Fact]
    public async Task NullableValue_RoundTripsThroughColumns()
    {
        var entity = TEntity.Create(First, Second);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        await AssertEntity(entity.Id, First, Second);
    }

    [Fact]
    public async Task FiltersAndOrdersByEndpointColumnInBothDatabases()
    {
        var first = TEntity.Create(First, null);
        var second = TEntity.Create(Second, null);
        SqlSet.AddRange(first, second);
        PgSet.AddRange(first, second);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        await AssertFilterAndOrder(PgSet, first.Id, second.Id);
        if (SqlServerAvailable)
        {
            await AssertFilterAndOrder(SqlSet, first.Id, second.Id);
        }
    }

    private async Task AssertFilterAndOrder(DbSet<TEntity> set, Guid firstId, Guid secondId)
    {
        var secondStart = Second.Start;
        var filtered = await set
            .Where(e => (e.Id == firstId || e.Id == secondId) && e.Value.Start == secondStart)
            .Select(e => e.Id)
            .ToListAsync(Ct);
        Assert.Equal([secondId], filtered);

        var ordered = await set
            .Where(e => e.Id == firstId || e.Id == secondId)
            .OrderBy(e => e.Value.Start)
            .Select(e => e.Id)
            .ToListAsync(Ct);
        Assert.Equal([firstId, secondId], ordered);
    }

    [Fact]
    public void ValueMapsToTwoEndpointColumnsWithoutFlagColumns() =>
        AssertEndpointColumns(nameof(IEntity<TEntity, FiniteInterval<int>, FiniteInterval<int>?>.Value), expectDiscriminator: false);

    [Fact]
    public void NullableValueMapsToTwoEndpointColumnsWithDiscriminator() =>
        AssertEndpointColumns(nameof(IEntity<TEntity, FiniteInterval<int>, FiniteInterval<int>?>.NullableValue), expectDiscriminator: true);

    private void AssertEndpointColumns(string propertyName, bool expectDiscriminator)
    {
        var entityType = PgDb.Model.FindEntityType(typeof(TEntity))!;
        var complex = entityType.GetComplexProperties().Single(p => p.Name == propertyName);
        var members = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();

        Assert.Contains(nameof(FiniteInterval<int>.Start), members);
        Assert.Contains(nameof(FiniteInterval<int>.End), members);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.StartInclusive), members);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.EndInclusive), members);
        Assert.Equal(expectDiscriminator, members.Contains("Discriminator"));
        Assert.DoesNotContain(entityType.GetProperties(), p => p.Name == propertyName);   // not a single scalar column
    }
}

[Collection(IntegrationTestCollection.Name)]
public sealed class ExplicitColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<ExplicitColumnsIntervalEntity>(factory)
{
    // HasIntervalColumns without a bound argument: the default is AlwaysInclusive, so bounds stay inclusive.
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysInclusiveColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysInclusiveColumnsIntervalEntity>(factory)
{
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysExclusiveColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysExclusiveColumnsIntervalEntity>(factory)
{
    // Both bounds AlwaysExclusive stores no flag column, so both endpoints must be exclusive.
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10, startInclusive: false, endInclusive: false);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20, startInclusive: false, endInclusive: false);

    [Fact]
    public async Task RejectsAnInclusiveBoundOnSave()
    {
        var db = (DbContext)PgDb;
        db.Add(AlwaysExclusiveColumnsIntervalEntity.Create(FiniteInterval.Create(1, 10), null));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        Assert.Contains("AlwaysExclusive", exception.Message);
        db.ChangeTracker.Clear();
    }
}
