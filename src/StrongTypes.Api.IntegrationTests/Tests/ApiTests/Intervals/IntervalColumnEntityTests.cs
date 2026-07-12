using System.Linq;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// EF-level round-trip suite for the two-scalar-column persistence shape
/// (<c>HasIntervalColumns</c> → EF Core complex type). The wire/JSON path is
/// identical to the JSON-column entities and already covered by
/// <see cref="IntervalEntityTests{TEntity, TInterval}"/>; what differs here is
/// storage, so these go straight through EF Core against both providers and
/// also assert the interval really maps to two columns rather than one.
/// </summary>
public abstract class IntervalColumnEntityTestsBase<TEntity, TInterval>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, TInterval, TInterval?>(factory)
    where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
    where TInterval : struct
{
    protected abstract TInterval ValidValue { get; }

    [Fact]
    public async Task RoundTripsThroughColumnsInBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, ValidValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        // Drop the tracked instances so AssertEntity re-materializes from the columns.
        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        await AssertEntity(entity.Id, ValidValue, ValidValue);
    }

    [Fact]
    public async Task RoundTripsWithNullNullableInBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        await AssertEntity(entity.Id, ValidValue, null);
    }

    [Fact]
    public void ValueMapsToTwoSeparateEndpointColumns()
    {
        var entityType = PgDb.Model.FindEntityType(typeof(TEntity))!;

        var value = entityType.GetComplexProperties().Single(p => p.Name == nameof(IEntity<TEntity, TInterval, TInterval?>.Value));
        var endpointColumns = value.ComplexType.GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains(nameof(FiniteInterval<int>.Start), endpointColumns);
        Assert.Contains(nameof(FiniteInterval<int>.End), endpointColumns);

        // Not a single scalar column (that would be the JSON shape).
        Assert.DoesNotContain(entityType.GetProperties(), p => p.Name == nameof(IEntity<TEntity, TInterval, TInterval?>.Value));
    }
}

[Collection(IntegrationTestCollection.Name)]
public sealed class FiniteIntervalColumnEntityTests(TestWebApplicationFactory factory)
    : IntervalColumnEntityTestsBase<FiniteIntervalColumnsEntity, FiniteInterval<int>>(factory)
{
    protected override FiniteInterval<int> ValidValue => FiniteInterval.Create(1, 10);

    // Representative for all variants: complex-type member access translates to the endpoint columns.
    [Fact]
    public async Task FiltersByEndpointColumnInBothDatabases()
    {
        var entity = FiniteIntervalColumnsEntity.Create(FiniteInterval.Create(41, 43), null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();

        var pgMatch = await PgSet.SingleAsync(e => e.Value.Start == 41 && e.Value.End == 43, Ct);
        Assert.Equal(entity.Id, pgMatch.Id);
        if (SqlServerAvailable)
        {
            var sqlMatch = await SqlSet.SingleAsync(e => e.Value.Start == 41 && e.Value.End == 43, Ct);
            Assert.Equal(entity.Id, sqlMatch.Id);
        }
    }
}

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalColumnEntityTests(TestWebApplicationFactory factory)
    : IntervalColumnEntityTestsBase<IntervalColumnsEntity, Interval<int>>(factory)
{
    protected override Interval<int> ValidValue => Interval.Create(1, 10);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalFromColumnEntityTests(TestWebApplicationFactory factory)
    : IntervalColumnEntityTestsBase<IntervalFromColumnsEntity, IntervalFrom<int>>(factory)
{
    protected override IntervalFrom<int> ValidValue => IntervalFrom.Create(1, null);
}

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalUntilColumnEntityTests(TestWebApplicationFactory factory)
    : IntervalColumnEntityTestsBase<IntervalUntilColumnsEntity, IntervalUntil<int>>(factory)
{
    protected override IntervalUntil<int> ValidValue => IntervalUntil.Create(null, 10);
}
