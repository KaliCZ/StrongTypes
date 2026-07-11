using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Issue #112 for intervals: a nullable interval in a non-public EF-mapped
/// backing property must round-trip with the two-endpoint-column shape wired
/// automatically, and its shadow discriminator must keep a <c>null</c> property
/// distinct from an unbounded interval on read. Without the convention's
/// complex-property hook the nullable interval loses its discriminator and a
/// stored <c>null</c> is indistinguishable from <c>(-∞, ∞)</c>. Runs against both
/// providers; the entity is outside the <c>IEntity</c> shape, so this drives the
/// DbContexts directly rather than through <c>IntegrationTestBase</c>.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class InternalBackingIntervalPropertyTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    private SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    private PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();
    private DbSet<InternalBackingIntervalEntity> SqlSet => SqlDb.Set<InternalBackingIntervalEntity>();
    private DbSet<InternalBackingIntervalEntity> PgSet => PgDb.Set<InternalBackingIntervalEntity>();
    private bool SqlServerAvailable => factory.SqlServerAvailable;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NonPublicIntervalBackingProperty_RoundTripsBoundedUnboundedAndNull()
    {
        var bounded = await Seed(Interval.Create(3, 9));
        var unbounded = await Seed(Interval.Create<int>(null, null));
        var empty = await Seed(null);

        await ForEachProvider(async set =>
        {
            var a = await set.AsNoTracking().SingleAsync(e => e.Id == bounded, Ct);
            Assert.Equal(Interval.Create(3, 9), a.ReadBacking());

            var b = await set.AsNoTracking().SingleAsync(e => e.Id == unbounded, Ct);
            Assert.Equal(Interval.Create<int>(null, null), b.ReadBacking());

            var c = await set.AsNoTracking().SingleAsync(e => e.Id == empty, Ct);
            Assert.Null(c.ReadBacking());
        });
    }

    private async Task<Guid> Seed(Interval<int>? window)
    {
        var entity = InternalBackingIntervalEntity.Create(window);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();
        return entity.Id;
    }

    private async Task ForEachProvider(Func<DbSet<InternalBackingIntervalEntity>, Task> assert)
    {
        await assert(PgSet);
        if (SqlServerAvailable)
        {
            await assert(SqlSet);
        }
    }

    public void Dispose() => _scope.Dispose();
}
