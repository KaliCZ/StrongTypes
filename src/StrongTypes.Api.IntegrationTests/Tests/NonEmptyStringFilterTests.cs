using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies EF Core translates strong-type predicates to server-side SQL on
/// both providers. Each test seeds rows with a unique prefix and scopes its
/// assertions to those rows, so it tolerates accumulated state from the
/// sibling CRUD suite on the shared fixture.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NonEmptyStringFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>(factory)
{
    protected override string RoutePrefix => "non-empty-string-entities";

    private string EqualToEndpoint(string provider, string value) =>
        $"/{RoutePrefix}/{provider}/equal-to?value={Uri.EscapeDataString(value)}";
    private string NotEqualToEndpoint(string provider, string value) =>
        $"/{RoutePrefix}/{provider}/not-equal-to?value={Uri.EscapeDataString(value)}";
    private string NullNullableEndpoint(string provider) =>
        $"/{RoutePrefix}/{provider}/null-nullable";
    private string NotNullNullableEndpoint(string provider) =>
        $"/{RoutePrefix}/{provider}/not-null-nullable";
    private string OrderedEndpoint(string provider) =>
        $"/{RoutePrefix}/{provider}/ordered";
    private string ContainsEndpoint(string provider, string term) =>
        $"/{RoutePrefix}/{provider}/contains?term={Uri.EscapeDataString(term)}";
    private string StartsWithEndpoint(string provider, string prefix) =>
        $"/{RoutePrefix}/{provider}/starts-with?prefix={Uri.EscapeDataString(prefix)}";
    private string EndsWithEndpoint(string provider, string suffix) =>
        $"/{RoutePrefix}/{provider}/ends-with?suffix={Uri.EscapeDataString(suffix)}";
    private string LikeEndpoint(string provider, string pattern) =>
        $"/{RoutePrefix}/{provider}/like?pattern={Uri.EscapeDataString(pattern)}";

    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    // Unique per-test prefix so assertions can scope to rows this test seeded,
    // even though the fixture is collection-scoped and shared with CRUD tests.
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

    private async Task<List<Guid>> GetIds(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<Guid>>(Ct))!;
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EqualTo_MatchesOnlyRowsWithThatValue(string provider)
    {
        var a = await Seed(Prefix + "alpha", null);
        var b = await Seed(Prefix + "beta", null);

        var ids = await GetIds(EqualToEndpoint(provider, Prefix + "alpha"));

        Assert.Contains(a, ids);
        Assert.DoesNotContain(b, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NotEqualTo_ExcludesRowsWithThatValue(string provider)
    {
        var a = await Seed(Prefix + "alpha", null);
        var b = await Seed(Prefix + "beta", null);

        var ids = await GetIds(NotEqualToEndpoint(provider, Prefix + "alpha"));

        Assert.DoesNotContain(a, ids);
        Assert.Contains(b, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NullNullable_IncludesRowsWithNullNullableValue(string provider)
    {
        var withNull = await Seed(Prefix + "null", null);
        var withValue = await Seed(Prefix + "nonnull", NonEmptyString.Create("x"));

        var ids = await GetIds(NullNullableEndpoint(provider));

        Assert.Contains(withNull, ids);
        Assert.DoesNotContain(withValue, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task NotNullNullable_IncludesRowsWithNonNullNullableValue(string provider)
    {
        var withNull = await Seed(Prefix + "null", null);
        var withValue = await Seed(Prefix + "nonnull", NonEmptyString.Create("x"));

        var ids = await GetIds(NotNullNullableEndpoint(provider));

        Assert.DoesNotContain(withNull, ids);
        Assert.Contains(withValue, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task Ordered_ReturnsRowsSortedByValue(string provider)
    {
        // Seed in non-alphabetical insertion order, assert alphabetical output order.
        var c = await Seed(Prefix + "c", null);
        var a = await Seed(Prefix + "a", null);
        var b = await Seed(Prefix + "b", null);

        var ids = await GetIds(OrderedEndpoint(provider));

        // Restrict to our three ids and assert their relative order matches
        // the sort of the seeded values: a, b, c.
        var ours = ids.Where(id => id == a || id == b || id == c).ToList();
        Assert.Equal(new[] { a, b, c }, ours);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task Contains_TranslatesUnwrapContainsToSql(string provider)
    {
        var match = await Seed(Prefix + "needle-haystack", null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await GetIds(ContainsEndpoint(provider, "needle"));

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task StartsWith_TranslatesUnwrapStartsWithToSql(string provider)
    {
        var match = await Seed(Prefix + "prefix-match", null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await GetIds(StartsWithEndpoint(provider, Prefix + "prefix"));

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EndsWith_TranslatesUnwrapEndsWithToSql(string provider)
    {
        var suffix = $"-{Guid.NewGuid():N}-tail";
        var match = await Seed(Prefix + "m" + suffix, null);
        var noMatch = await Seed(Prefix + "other", null);

        var ids = await GetIds(EndsWithEndpoint(provider, suffix));

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    // The user-facing ask: EF.Functions.Like must translate when applied to
    // the unwrapped string of a strong-type column. Exercise it directly.
    [Theory, MemberData(nameof(Providers))]
    public async Task Like_TranslatesEfFunctionsLikeToSql(string provider)
    {
        var match = await Seed(Prefix + "apple", null);
        var noMatch = await Seed(Prefix + "banana", null);

        var ids = await GetIds(LikeEndpoint(provider, Prefix + "app%"));

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }
}
