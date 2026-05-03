using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies <c>MailAddress.Unwrap()</c> (plus equality and EF.Functions.Like)
/// translates to server-side SQL on both providers when applied to the
/// <see cref="EmailEntity"/>'s <see cref="MailAddress"/> column. Tests query
/// the <see cref="DbContext"/> directly so the LINQ translator is what we
/// exercise — no HTTP plumbing in the way. Each test seeds rows with a
/// unique local-part prefix and scopes assertions to those rows so it
/// tolerates accumulated state from sibling tests on the shared fixture.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MailAddressFilterTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<EmailEntity, MailAddress, MailAddress?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    private DbSet<EmailEntity> Set(string provider) =>
        provider == "sql-server" ? SqlSet : PgSet;

    // Unique per-test local-part prefix so each test's assertions are
    // isolated from other tests' rows on the collection-scoped database.
    private string Prefix { get; } = $"flt-{Guid.NewGuid():N}-";

    private async Task<Guid> Seed(string localPart, MailAddress? nullableValue)
    {
        var entity = EmailEntity.Create(MailAddress.Create($"{Prefix}{localPart}@example.com"), nullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        return entity.Id;
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task EqualTo_TranslatesToSql(string provider)
    {
        var a = await Seed("alpha", null);
        var b = await Seed("beta", null);
        var needle = MailAddress.Create($"{Prefix}alpha@example.com");

        var ids = await Set(provider).Where(e => e.Value == needle).Select(e => e.Id).ToListAsync(Ct);

        Assert.Contains(a, ids);
        Assert.DoesNotContain(b, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task UnwrapStartsWith_TranslatesToSql(string provider)
    {
        var match = await Seed("prefix-match", null);
        var noMatch = await Seed("other", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix + "prefix"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task UnwrapContains_TranslatesToSql(string provider)
    {
        var match = await Seed("needle-haystack", null);
        var noMatch = await Seed("other", null);

        var ids = await Set(provider)
            .Where(e => e.Value.Unwrap().StartsWith(Prefix) && e.Value.Unwrap().Contains("needle"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }

    // The load-bearing case: EF.Functions.Like must translate when applied
    // to Unwrap() of the MailAddress column.
    [Theory, MemberData(nameof(Providers))]
    public async Task EfFunctionsLike_TranslatesToSql(string provider)
    {
        var match = await Seed("apple", null);
        var noMatch = await Seed("banana", null);

        var ids = await Set(provider)
            .Where(e => EF.Functions.Like(e.Value.Unwrap(), Prefix + "app%"))
            .Select(e => e.Id)
            .ToListAsync(Ct);

        Assert.Contains(match, ids);
        Assert.DoesNotContain(noMatch, ids);
    }
}
