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

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

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
