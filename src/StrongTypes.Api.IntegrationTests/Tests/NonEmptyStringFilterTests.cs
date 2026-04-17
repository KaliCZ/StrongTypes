using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies <c>NonEmptyString.Unwrap()</c> (plus equality, null checks, and
/// ordering on the strong type) translates to server-side SQL on both
/// providers. Tests query the <see cref="DbContext"/> directly — the LINQ
/// translator is what we're exercising, no HTTP plumbing in the way.
/// Each test seeds rows with a unique prefix and scopes assertions to those
/// rows so it tolerates accumulated state from sibling tests on the shared
/// fixture.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NonEmptyStringFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    private DbSet<NonEmptyStringEntity> Set(string provider) =>
        provider == "sql-server" ? SqlSet : PgSet;

    // Unique per-test prefix so each test's assertions are isolated from
    // other tests' rows on the collection-scoped database.
    private string Prefix { get; } = $"flt-{Guid.NewGuid():N}-";

    private async Task<Guid> Seed(string value, NonEmptyString? nullableValue)
    {
        var entity = NonEmptyStringEntity.Create(NonEmptyString.Create(value), nullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        return entity.Id;
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EqualTo_TranslatesToSql(string provider)
    {
        var a = await Seed(Prefix + "alpha", null);
        var b = await Seed(Prefix + "beta", null);
        var needle = NonEmptyString.Create(Prefix + "alpha");

        var ids = await Set(provider).Where(e => e.Value == needle).Select(e => e.Id).ToListAsync(Ct);

        Assert.Contains(a, ids);
        Assert.DoesNotContain(b, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NotEqualTo_TranslatesToSql(string provider)
    {
        var a = await Seed(Prefix + "alpha", null);
        var b = await Seed(Prefix + "beta", null);
        var needle = NonEmptyString.Create(Prefix + "alpha");

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.Value != needle)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(a, ids);
        Assert.Contains(b, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NullNullable_TranslatesToSql(string provider)
    {
        var withNull = await Seed(Prefix + "null", null);
        var withValue = await Seed(Prefix + "nonnull", NonEmptyString.Create("x"));

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.NullableValue == null)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(withNull, ids);
        Assert.DoesNotContain(withValue, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NotNullNullable_TranslatesToSql(string provider)
    {
        var withNull = await Seed(Prefix + "null", null);
        var withValue = await Seed(Prefix + "nonnull", NonEmptyString.Create("x"));

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.NullableValue != null)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.DoesNotContain(withNull, ids);
        Assert.Contains(withValue, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task OrderBy_TranslatesToSql(string provider)
    {
        var c = await Seed(Prefix + "c", null);
        var a = await Seed(Prefix + "a", null);
        var b = await Seed(Prefix + "b", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix))
            .OrderBy(e => e.Value)
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Equal(new[] { a, b, c }, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task UnwrapContains_TranslatesToSql(string provider)
    {
        var match = await Seed(Prefix + "needle-haystack", null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.Value.Unwrap().Contains("needle"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task UnwrapStartsWith_TranslatesToSql(string provider)
    {
        var match = await Seed(Prefix + "prefix-match", null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix + "prefix"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task UnwrapEndsWith_TranslatesToSql(string provider)
    {
        var suffix = $"-{Guid.NewGuid():N}-tail";
        var match = await Seed(Prefix + "m" + suffix, null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.Value.Unwrap().EndsWith(suffix))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    // The load-bearing case from the issue: EF.Functions.Like must translate
    // when applied to Unwrap() of a strong-type column.
    [Theory, MemberData(nameof(Providers))]
    public async Task EfFunctionsLike_TranslatesToSql(string provider)
    {
        var match = await Seed(Prefix + "apple", null);
        var noMatch = await Seed(Prefix + "banana", null);

        var ids = await Set(provider)
            .Where(e => EF.Functions.Like(e.Value.Unwrap(), Prefix + "app%"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }
}
