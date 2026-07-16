using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

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

    /// <summary>False only when the host opted into skipping SQL Server — gate SQL-Server-only assertions on this; see testing.md.</summary>
    protected bool SqlServerAvailable => factory.SqlServerAvailable;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Skipped SQL Server is not asserted against — a match on the in-memory stub would be a false pass.</summary>
    protected async Task AssertEntity(Guid id, T expectedValue, TNullable expectedNullableValue)
    {
        await AssertEntity(PgSet, id, expectedValue, expectedNullableValue);
        if (SqlServerAvailable)
        {
            await AssertEntity(SqlSet, id, expectedValue, expectedNullableValue);
        }
    }

    protected void SkipIfSqlServerUnavailable(string provider) =>
        Assert.SkipWhen(provider == "sql-server" && !SqlServerAvailable, "SQL Server is not available on this host.");

    private static async Task AssertEntity(
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
