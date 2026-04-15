using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides an HttpClient and both DbContexts
/// via a per-test scope so individual tests don't have to create scopes manually.
/// </summary>
public abstract class IntegrationTestBase(TestWebApplicationFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    /// <summary>
    /// Fetches the entity with the given id from the supplied DbContext and asserts
    /// that its Value and NullableValue match the expected values.
    /// </summary>
    protected static async Task AssertStringEntity(
        DbContext db,
        Guid id,
        string expectedValue,
        string? expectedNullableValue)
    {
        var ct = TestContext.Current.CancellationToken;
        var entity = await db.Set<StringEntity>().FindAsync([id], ct);
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
