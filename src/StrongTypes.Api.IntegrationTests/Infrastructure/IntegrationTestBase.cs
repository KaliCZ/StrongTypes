using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Generic base for any <see cref="IEntity{TSelf, T, TNullable}"/>. Supplies
/// the HTTP Client, both DbContexts, the current CancellationToken, and the
/// generic <see cref="AssertEntity"/> helper that reads from a
/// <see cref="DbSet{TEntity}"/>. HTTP route/body helpers live on the
/// subclass that actually uses them (<c>EntityTests</c>).
/// </summary>
public abstract class IntegrationTestBase<TEntity, T, TNullable>(TestWebApplicationFactory factory) : IDisposable
    where TEntity : class, IEntity<TEntity, T, TNullable>
    where T : notnull
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    protected DbSet<TEntity> SqlSet => SqlDb.Set<TEntity>();
    protected DbSet<TEntity> PgSet => PgDb.Set<TEntity>();

    /// <summary>
    /// Whether the SQL Server provider is backed by a real SQL Server on this host.
    /// <see langword="false"/> only on a local host that opted into skipping SQL Server.
    /// Guard every SQL-Server-specific assertion with this.
    /// </summary>
    protected bool SqlServerAvailable => factory.SqlServerAvailable;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Asserts the SQL Server row matches when SQL Server is available; a no-op
    /// otherwise. The in-memory stub used when SQL Server is skipped does not
    /// exercise the real wire path, so asserting against it would be a false pass.
    /// </summary>
    protected async Task AssertSqlServerEntity(Guid id, T expectedValue, TNullable expectedNullableValue)
    {
        if (!SqlServerAvailable)
        {
            return;
        }
        await AssertEntity(SqlSet, id, expectedValue, expectedNullableValue);
    }

    /// <summary>
    /// Skips a provider-parametrized test for the <c>sql-server</c> provider when
    /// SQL Server is unavailable on this host; a no-op for any other provider.
    /// </summary>
    protected void SkipIfSqlServerUnavailable(string provider) =>
        Assert.SkipWhen(provider == "sql-server" && !SqlServerAvailable, "SQL Server is not available on this host.");

    /// <summary>
    /// Fetches the entity with the given id from the supplied DbSet and asserts
    /// that its Value and NullableValue match the expected values.
    /// </summary>
    protected static async Task AssertEntity(
        DbSet<TEntity> set,
        Guid id,
        T expectedValue,
        TNullable expectedNullableValue)
    {
        var entity = await set.FindAsync([id], Ct);
        Assert.NotNull(entity);
        Assert.Equal(expectedValue, entity!.Value);
        Assert.Equal(expectedNullableValue, entity.NullableValue);
    }

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
    }
}
