using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Two-column persistence matrix for the explicit <c>HasIntervalColumns</c> entry
/// points — the no-argument default, both bounds <see cref="IntervalBoundMode.AlwaysInclusive"/>,
/// and both bounds <see cref="IntervalBoundMode.AlwaysExclusive"/> — across all four
/// interval variants, each for a non-nullable and a nullable property. Every config
/// round-trips through the real endpoint columns on both providers and asserts the
/// two-column shape (Start/End, discriminator on the nullable form, no flag columns).
/// Endpoint filter/order lives on the <see cref="FiniteInterval{T}"/> subclasses (via
/// <see cref="FiniteIntervalColumnMatrixTestsBase{TEntity}"/>) since it needs typed
/// endpoint access; the column translation itself is variant-agnostic and covered for
/// the other variants by <c>IntervalFilterTests</c>. The remaining shapes are covered
/// alongside: the convention two-column default by <see cref="IntervalColumnEntityTests"/>,
/// the single JSON column by <see cref="IntervalEntityTests{TEntity, TInterval}"/>, and
/// per-value <see cref="IntervalBoundMode.Stored"/> bounds by <see cref="IntervalBoundModeTests"/>.
/// </summary>
public abstract class IntervalColumnMappingMatrixTestsBase<TEntity, TInterval>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, TInterval, TInterval?>(factory)
    where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
    where TInterval : struct
{
    /// <summary>Two values valid under this config's bound mode, ordered so <c>First.Start &lt; Second.Start</c>.</summary>
    protected abstract TInterval First { get; }
    protected abstract TInterval Second { get; }

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
    public void ValueMapsToTwoEndpointColumnsWithoutFlagColumns() =>
        AssertEndpointColumns(nameof(IEntity<TEntity, TInterval, TInterval?>.Value), expectDiscriminator: false);

    [Fact]
    public void NullableValueMapsToTwoEndpointColumnsWithDiscriminator() =>
        AssertEndpointColumns(nameof(IEntity<TEntity, TInterval, TInterval?>.NullableValue), expectDiscriminator: true);

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

    /// <summary>Shared by the <c>AlwaysExclusive</c> subclasses: saving a value with an inclusive bound has no flag column to hold it and must throw.</summary>
    protected async Task AssertRejectsInclusiveSave(TInterval inclusiveValue)
    {
        var db = (DbContext)PgDb;
        db.Add(TEntity.Create(inclusiveValue, null));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
        Assert.Contains("AlwaysExclusive", exception.Message);
        db.ChangeTracker.Clear();
    }

    /// <summary>The guard must run on update too, not just insert: changing a stored value to one with an inclusive bound must throw. <paramref name="inclusiveValue"/> must differ in its endpoints so EF registers the interval as modified (the inclusivity flags have no column of their own here).</summary>
    protected async Task AssertRejectsInclusiveUpdate(TInterval seedExclusive, TInterval inclusiveValue)
    {
        var entity = TEntity.Create(seedExclusive, null);
        PgSet.Add(entity);
        await PgDb.SaveChangesAsync(Ct);
        PgDb.ChangeTracker.Clear();

        var loaded = await PgSet.FirstAsync(e => e.Id == entity.Id, Ct);
        loaded.Value = inclusiveValue;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => PgDb.SaveChangesAsync(Ct));
        Assert.Contains("AlwaysExclusive", exception.Message);
        PgDb.ChangeTracker.Clear();
    }
}

/// <summary>Adds endpoint filter/order coverage for the <see cref="FiniteInterval{T}"/> configs, where the endpoint is a plain non-nullable column.</summary>
public abstract class FiniteIntervalColumnMatrixTestsBase<TEntity>(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<TEntity, FiniteInterval<int>>(factory)
    where TEntity : class, IEntity<TEntity, FiniteInterval<int>, FiniteInterval<int>?>
{
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
}

// ── FiniteInterval<int> ───────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class ExplicitColumnsFiniteIntervalMatrixTests(TestWebApplicationFactory factory)
    : FiniteIntervalColumnMatrixTestsBase<ExplicitColumnsFiniteIntervalEntity>(factory)
{
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysInclusiveColumnsFiniteIntervalMatrixTests(TestWebApplicationFactory factory)
    : FiniteIntervalColumnMatrixTestsBase<AlwaysInclusiveColumnsFiniteIntervalEntity>(factory)
{
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysExclusiveColumnsFiniteIntervalMatrixTests(TestWebApplicationFactory factory)
    : FiniteIntervalColumnMatrixTestsBase<AlwaysExclusiveColumnsFiniteIntervalEntity>(factory)
{
    protected override FiniteInterval<int> First => FiniteInterval.Create(1, 10, startInclusive: false, endInclusive: false);
    protected override FiniteInterval<int> Second => FiniteInterval.Create(5, 20, startInclusive: false, endInclusive: false);

    [Fact]
    public Task RejectsAnInclusiveBoundOnSave() => AssertRejectsInclusiveSave(FiniteInterval.Create(1, 10));

    [Fact]
    public Task RejectsAnInclusiveBoundOnUpdate() => AssertRejectsInclusiveUpdate(First, FiniteInterval.Create(5, 20));
}

// ── IntervalFrom<int> ─────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class ExplicitColumnsIntervalFromMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<ExplicitColumnsIntervalFromEntity, IntervalFrom<int>>(factory)
{
    protected override IntervalFrom<int> First => IntervalFrom.Create(1, 10);
    protected override IntervalFrom<int> Second => IntervalFrom.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysInclusiveColumnsIntervalFromMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysInclusiveColumnsIntervalFromEntity, IntervalFrom<int>>(factory)
{
    protected override IntervalFrom<int> First => IntervalFrom.Create(1, 10);
    protected override IntervalFrom<int> Second => IntervalFrom.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysExclusiveColumnsIntervalFromMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysExclusiveColumnsIntervalFromEntity, IntervalFrom<int>>(factory)
{
    protected override IntervalFrom<int> First => IntervalFrom.Create(1, 10, startInclusive: false, endInclusive: false);
    protected override IntervalFrom<int> Second => IntervalFrom.Create(5, 20, startInclusive: false, endInclusive: false);

    [Fact]
    public Task RejectsAnInclusiveBoundOnSave() => AssertRejectsInclusiveSave(IntervalFrom.Create(1, 10));

    [Fact]
    public Task RejectsAnInclusiveBoundOnUpdate() => AssertRejectsInclusiveUpdate(First, IntervalFrom.Create(5, 20));
}

// ── IntervalUntil<int> ────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class ExplicitColumnsIntervalUntilMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<ExplicitColumnsIntervalUntilEntity, IntervalUntil<int>>(factory)
{
    protected override IntervalUntil<int> First => IntervalUntil.Create(1, 10);
    protected override IntervalUntil<int> Second => IntervalUntil.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysInclusiveColumnsIntervalUntilMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysInclusiveColumnsIntervalUntilEntity, IntervalUntil<int>>(factory)
{
    protected override IntervalUntil<int> First => IntervalUntil.Create(1, 10);
    protected override IntervalUntil<int> Second => IntervalUntil.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysExclusiveColumnsIntervalUntilMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysExclusiveColumnsIntervalUntilEntity, IntervalUntil<int>>(factory)
{
    protected override IntervalUntil<int> First => IntervalUntil.Create(1, 10, startInclusive: false, endInclusive: false);
    protected override IntervalUntil<int> Second => IntervalUntil.Create(5, 20, startInclusive: false, endInclusive: false);

    [Fact]
    public Task RejectsAnInclusiveBoundOnSave() => AssertRejectsInclusiveSave(IntervalUntil.Create(1, 10));

    [Fact]
    public Task RejectsAnInclusiveBoundOnUpdate() => AssertRejectsInclusiveUpdate(First, IntervalUntil.Create(5, 20));
}

// ── Interval<int> ─────────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class ExplicitColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<ExplicitColumnsIntervalEntity, Interval<int>>(factory)
{
    protected override Interval<int> First => Interval.Create(1, 10);
    protected override Interval<int> Second => Interval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysInclusiveColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysInclusiveColumnsIntervalEntity, Interval<int>>(factory)
{
    protected override Interval<int> First => Interval.Create(1, 10);
    protected override Interval<int> Second => Interval.Create(5, 20);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class AlwaysExclusiveColumnsIntervalMatrixTests(TestWebApplicationFactory factory)
    : IntervalColumnMappingMatrixTestsBase<AlwaysExclusiveColumnsIntervalEntity, Interval<int>>(factory)
{
    protected override Interval<int> First => Interval.Create(1, 10, startInclusive: false, endInclusive: false);
    protected override Interval<int> Second => Interval.Create(5, 20, startInclusive: false, endInclusive: false);

    [Fact]
    public Task RejectsAnInclusiveBoundOnSave() => AssertRejectsInclusiveSave(Interval.Create(1, 10));

    [Fact]
    public Task RejectsAnInclusiveBoundOnUpdate() => AssertRejectsInclusiveUpdate(First, Interval.Create(5, 20));
}
