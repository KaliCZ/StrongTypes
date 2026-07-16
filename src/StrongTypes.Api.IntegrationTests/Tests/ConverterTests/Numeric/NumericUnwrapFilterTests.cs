using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// One representative arithmetic case per wrapper — the translation is identical across shapes.
/// Predicates cast to <c>long</c> because sibling suites seed <c>int.MaxValue</c>/<c>MinValue</c>
/// rows into the shared tables and SQL Server does not short-circuit, so 32-bit arithmetic
/// would overflow on those rows before the ID filter excludes them.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NumericUnwrapFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<PositiveIntEntity, Positive<int>, Positive<int>?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    [Theory, MemberData(nameof(Providers))]
    public async Task Positive_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var small = PositiveIntEntity.Create(Positive<int>.Create(3), null);
        var large = PositiveIntEntity.Create(Positive<int>.Create(7), null);
        SqlDb.Add(small); SqlDb.Add(large);
        PgDb.Add(small); PgDb.Add(large);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.PositiveIntEntities : PgDb.PositiveIntEntities;

        var ids = await set
            .Where(e => (e.Id == small.Id || e.Id == large.Id) && (long)e.Value.Unwrap() * 2 > 10)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(small.Id, ids);
        Assert.Contains(large.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NonNegative_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var zero = NonNegativeIntEntity.Create(NonNegative<int>.Create(0), null);
        var big = NonNegativeIntEntity.Create(NonNegative<int>.Create(10), null);
        SqlDb.Add(zero); SqlDb.Add(big);
        PgDb.Add(zero); PgDb.Add(big);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NonNegativeIntEntities : PgDb.NonNegativeIntEntities;

        var ids = await set
            .Where(e => (e.Id == zero.Id || e.Id == big.Id) && (long)e.Value.Unwrap() + 5 > 6)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(zero.Id, ids);
        Assert.Contains(big.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task Negative_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var small = NegativeIntEntity.Create(Negative<int>.Create(-1), null);
        var large = NegativeIntEntity.Create(Negative<int>.Create(-100), null);
        SqlDb.Add(small); SqlDb.Add(large);
        PgDb.Add(small); PgDb.Add(large);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NegativeIntEntities : PgDb.NegativeIntEntities;

        var ids = await set
            .Where(e => (e.Id == small.Id || e.Id == large.Id) && (long)e.Value.Unwrap() < -10)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(small.Id, ids);
        Assert.Contains(large.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NonPositive_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        SkipIfSqlServerUnavailable(provider);

        var zero = NonPositiveIntEntity.Create(NonPositive<int>.Create(0), null);
        var negative = NonPositiveIntEntity.Create(NonPositive<int>.Create(-5), null);
        SqlDb.Add(zero); SqlDb.Add(negative);
        PgDb.Add(zero); PgDb.Add(negative);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NonPositiveIntEntities : PgDb.NonPositiveIntEntities;

        var ids = await set
            .Where(e => (e.Id == zero.Id || e.Id == negative.Id) && (long)e.Value.Unwrap() - 1 < -3)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(zero.Id, ids);
        Assert.Contains(negative.Id, ids);
    }
}
