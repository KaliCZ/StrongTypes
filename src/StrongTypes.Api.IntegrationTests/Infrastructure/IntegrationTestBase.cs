using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;

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

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
    }
}
