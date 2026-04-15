using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Generic infrastructure for every integration test: scoped DbContexts,
/// an HttpClient, and the current test's CancellationToken.
/// Entity-specific helpers live on derived bases (e.g. StringEntityTestsBase).
/// </summary>
public abstract class IntegrationTestBase(TestWebApplicationFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Builds the { Value, NullableValue } request body used by every write endpoint.
    /// Generic so future entities with other scalar types reuse the same shape.
    /// </summary>
    protected static object Body<T>(T value, T nullableValue) =>
        new { Value = value, NullableValue = nullableValue };

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
    }
}
