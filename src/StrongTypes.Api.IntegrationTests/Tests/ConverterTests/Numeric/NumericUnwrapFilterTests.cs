using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies <c>Unwrap()</c> on the source-generated numeric wrappers translates
/// to a plain-int column reference, so LINQ predicates on the raw value
/// (arithmetic, <c>Math.Abs</c>, etc.) evaluate server-side on both providers.
/// One representative case per wrapper type — the translator logic is identical
/// across shapes, so we don't need to matrix every arithmetic operator.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NumericUnwrapFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<PositiveIntEntity, Positive<int>, Positive<int>?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    [Theory, MemberData(nameof(Providers))]
    public async Task Positive_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        var small = PositiveIntEntity.Create(Positive<int>.Create(3), null);
        var large = PositiveIntEntity.Create(Positive<int>.Create(7), null);
        SqlDb.Add(small); SqlDb.Add(large);
        PgDb.Add(small); PgDb.Add(large);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.PositiveIntEntities : PgDb.PositiveIntEntities;

        var ids = await set
            .Where(e => (e.Id == small.Id || e.Id == large.Id) && e.Value.Unwrap() * 2 > 10)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(small.Id, ids);
        Assert.Contains(large.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NonNegative_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        var zero = NonNegativeIntEntity.Create(NonNegative<int>.Create(0), null);
        var big = NonNegativeIntEntity.Create(NonNegative<int>.Create(10), null);
        SqlDb.Add(zero); SqlDb.Add(big);
        PgDb.Add(zero); PgDb.Add(big);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NonNegativeIntEntities : PgDb.NonNegativeIntEntities;

        var ids = await set
            .Where(e => (e.Id == zero.Id || e.Id == big.Id) && e.Value.Unwrap() + 5 > 6)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(zero.Id, ids);
        Assert.Contains(big.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task Negative_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        var small = NegativeIntEntity.Create(Negative<int>.Create(-1), null);
        var large = NegativeIntEntity.Create(Negative<int>.Create(-100), null);
        SqlDb.Add(small); SqlDb.Add(large);
        PgDb.Add(small); PgDb.Add(large);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NegativeIntEntities : PgDb.NegativeIntEntities;

        var ids = await set
            .Where(e => (e.Id == small.Id || e.Id == large.Id) && e.Value.Unwrap() < -10)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(small.Id, ids);
        Assert.Contains(large.Id, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NonPositive_UnwrapArithmetic_TranslatesToSql(string provider)
    {
        var zero = NonPositiveIntEntity.Create(NonPositive<int>.Create(0), null);
        var negative = NonPositiveIntEntity.Create(NonPositive<int>.Create(-5), null);
        SqlDb.Add(zero); SqlDb.Add(negative);
        PgDb.Add(zero); PgDb.Add(negative);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var set = provider == "sql-server" ? SqlDb.NonPositiveIntEntities : PgDb.NonPositiveIntEntities;

        var ids = await set
            .Where(e => (e.Id == zero.Id || e.Id == negative.Id) && e.Value.Unwrap() - 1 < -3)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(zero.Id, ids);
        Assert.Contains(negative.Id, ids);
    }
}
