using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Issue #112: a nullable strong type held in a non-public EF-mapped backing
/// property must round-trip and be queryable with its value converter wired
/// automatically (no manual <c>HasConversion</c>). Were the converter missing,
/// the model would fail to build ("could not be mapped"). Runs against both
/// providers. The entity is outside the <c>IEntity</c> shape, so this test drives
/// the DbContexts directly rather than through <c>IntegrationTestBase</c>.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class InternalBackingPropertyTests(TestWebApplicationFactory factory) : IDisposable
{
    private const string BackingColumn = "BackingNullable";

    private readonly IServiceScope _scope = factory.Services.CreateScope();

    private SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    private PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();
    private DbSet<InternalBackingEntity> SqlSet => SqlDb.Set<InternalBackingEntity>();
    private DbSet<InternalBackingEntity> PgSet => PgDb.Set<InternalBackingEntity>();
    private bool SqlServerAvailable => factory.SqlServerAvailable;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NonPublicBackingProperty_RoundTripsNonNullAndNullThroughConverter()
    {
        var populated = await Seed("Acme", NonEmptyString.Create("acme|acme-inc"));
        var empty = await Seed("Beta", backing: null);

        await ForEachProvider(async set =>
        {
            var a = await set.AsNoTracking().SingleAsync(e => e.Id == populated, Ct);
            Assert.Equal(NonEmptyString.Create("acme|acme-inc"), a.ReadBacking());

            var b = await set.AsNoTracking().SingleAsync(e => e.Id == empty, Ct);
            Assert.Null(b.ReadBacking());
        });
    }

    [Fact]
    public async Task NonPublicBackingProperty_FilterByNullAndNotNull_TranslatesToSql()
    {
        var populated = await Seed("Acme", NonEmptyString.Create("acme|acme-inc"));
        var empty = await Seed("Beta", backing: null);
        Guid[] seeded = [populated, empty];

        await ForEachProvider(async set =>
        {
            var nulls = await set.Where(e => seeded.Contains(e.Id))
                .Where(e => EF.Property<NonEmptyString?>(e, BackingColumn) == null)
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Contains(empty, nulls);
            Assert.DoesNotContain(populated, nulls);

            var nonNulls = await set.Where(e => seeded.Contains(e.Id))
                .Where(e => EF.Property<NonEmptyString?>(e, BackingColumn) != null)
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Contains(populated, nonNulls);
            Assert.DoesNotContain(empty, nonNulls);
        });
    }

    private async Task<Guid> Seed(string name, NonEmptyString? backing)
    {
        var entity = InternalBackingEntity.Create(NonEmptyString.Create(name), backing);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();
        return entity.Id;
    }

    private async Task ForEachProvider(Func<DbSet<InternalBackingEntity>, Task> assert)
    {
        await assert(PgSet);
        if (SqlServerAvailable)
        {
            await assert(SqlSet);
        }
    }

    public void Dispose() => _scope.Dispose();
}
