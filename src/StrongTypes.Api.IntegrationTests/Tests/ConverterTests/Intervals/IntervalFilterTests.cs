using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies endpoint access on intervals translates to server-side SQL on both
/// providers, in both persistence shapes: two endpoint columns
/// (<c>HasIntervalColumns</c>, an EF Core complex type) and the default single
/// JSON column (translated to a JSON path lookup). Queries are scoped to the
/// rows each test seeds, since the collection shares one database.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<FiniteIntervalColumnsEntity, FiniteInterval<int>, FiniteInterval<int>?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    private async Task<TEntity> Seed<TEntity, TInterval>(DbSet<TEntity> sqlSet, DbSet<TEntity> pgSet, TInterval value, TInterval? nullableValue = null)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = TEntity.Create(value, nullableValue);
        sqlSet.Add(entity);
        pgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        return entity;
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EndpointEquality_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var match = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(1, 10));
        var noMatch = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(2, 10));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalColumnsEntities : PgDb.FiniteIntervalColumnsEntities;

        var ids = await set
            .FilterById(match, noMatch)
            .Where(e => e.Value.Start == 1)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match.Id, ids);
        Assert.DoesNotContain(noMatch.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task RangeContainment_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var containing = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(1, 10));
        var disjoint = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(20, 30));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalColumnsEntities : PgDb.FiniteIntervalColumnsEntities;

        var ids = await set
            .FilterById(containing, disjoint)
            .Where(e => e.Value.Start <= 5 && 5 <= e.Value.End)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(containing.Id, ids);
        Assert.DoesNotContain(disjoint.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task OrderByEndpoint_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var third = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(30, 40));
        var first = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(10, 40));
        var second = await Seed(SqlDb.FiniteIntervalColumnsEntities, PgDb.FiniteIntervalColumnsEntities, FiniteInterval.Create(20, 40));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalColumnsEntities : PgDb.FiniteIntervalColumnsEntities;

        var ids = await set
            .FilterById(first, second, third)
            .OrderBy(e => e.Value.Start)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Equal([first.Id, second.Id, third.Id], ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task OpenEndpointIsNull_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var open = await Seed(SqlDb.IntervalFromColumnsEntities, PgDb.IntervalFromColumnsEntities, IntervalFrom.Create(1, null));
        var bounded = await Seed(SqlDb.IntervalFromColumnsEntities, PgDb.IntervalFromColumnsEntities, IntervalFrom.Create(1, 10));
        var set = provider == "sql-server" ? SqlDb.IntervalFromColumnsEntities : PgDb.IntervalFromColumnsEntities;

        var ids = await set
            .FilterById(open, bounded)
            .Where(e => e.Value.End == null)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(open.Id, ids);
        Assert.DoesNotContain(bounded.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task StoredBoundFlag_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var halfOpen = await Seed(
            SqlDb.StoredBoundsIntervalEntities, PgDb.StoredBoundsIntervalEntities, FiniteInterval.Create(1, 10, endInclusive: false));
        var inclusive = await Seed(SqlDb.StoredBoundsIntervalEntities, PgDb.StoredBoundsIntervalEntities, FiniteInterval.Create(1, 10));
        var set = provider == "sql-server" ? SqlDb.StoredBoundsIntervalEntities : PgDb.StoredBoundsIntervalEntities;

        var ids = await set
            .FilterById(halfOpen, inclusive)
            .Where(e => !e.Value.EndInclusive)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(halfOpen.Id, ids);
        Assert.DoesNotContain(inclusive.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EndpointEquality_OnJsonColumn_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var match = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(1, 10));
        var noMatch = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(2, 10));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalEntities : PgDb.FiniteIntervalEntities;

        var ids = await set
            .FilterById(match, noMatch)
            .Where(e => e.Value.Start == 1)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match.Id, ids);
        Assert.DoesNotContain(noMatch.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task RangeContainment_OnJsonColumn_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var containing = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(1, 10));
        var disjoint = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(20, 30));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalEntities : PgDb.FiniteIntervalEntities;

        var ids = await set
            .FilterById(containing, disjoint)
            .Where(e => e.Value.Start <= 5 && 5 <= e.Value.End)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(containing.Id, ids);
        Assert.DoesNotContain(disjoint.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task OrderByEndpoint_OnJsonColumn_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var third = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(30, 40));
        var first = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(10, 40));
        var second = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(20, 40));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalEntities : PgDb.FiniteIntervalEntities;

        var ids = await set
            .FilterById(first, second, third)
            .OrderBy(e => e.Value.Start)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Equal([first.Id, second.Id, third.Id], ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task OpenEndpointIsNull_OnJsonColumn_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var open = await Seed(SqlDb.IntervalFromEntities, PgDb.IntervalFromEntities, IntervalFrom.Create(1, null));
        var bounded = await Seed(SqlDb.IntervalFromEntities, PgDb.IntervalFromEntities, IntervalFrom.Create(1, 10));
        var set = provider == "sql-server" ? SqlDb.IntervalFromEntities : PgDb.IntervalFromEntities;

        var ids = await set
            .FilterById(open, bounded)
            .Where(e => e.Value.End == null)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(open.Id, ids);
        Assert.DoesNotContain(bounded.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EndpointOnNullableJsonColumn_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var populated = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(1, 10), FiniteInterval.Create(3, 4));
        var empty = await Seed(SqlDb.FiniteIntervalEntities, PgDb.FiniteIntervalEntities, FiniteInterval.Create(1, 10));
        var set = provider == "sql-server" ? SqlDb.FiniteIntervalEntities : PgDb.FiniteIntervalEntities;

        var ids = await set
            .FilterById(populated, empty)
            .Where(e => e.NullableValue!.Value.Start == 3)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(populated.Id, ids);
        Assert.DoesNotContain(empty.Id, ids);
    }
}
