using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests. Provides Client, both DbContexts, the
/// current CancellationToken, the StringEntity routes and HTTP wrappers, and
/// the AssertStringEntity helper. Will be made generic over the entity type
/// in a follow-up PR.
/// </summary>
public abstract class IntegrationTestBase(TestWebApplicationFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected const string NonNullable = "/string-entities/non-nullable";
    protected const string Nullable = "/string-entities/nullable";
    protected static string UpdateNonNullable(Guid id) => $"/string-entities/{id}/non-nullable";
    protected static string UpdateNullable(Guid id) => $"/string-entities/{id}/nullable";
    protected static string SqlServerGet(Guid id) => $"/string-entities/{id}/sql-server";
    protected static string PostgreSqlGet(Guid id) => $"/string-entities/{id}/postgresql";

    /// <summary>
    /// Builds the { Value, NullableValue } request body used by every write endpoint.
    /// Generic so future entities with other scalar types reuse the same shape.
    /// </summary>
    protected static object Body<T>(T value, T? nullableValue) => new { Value = value, NullableValue = nullableValue };

    protected async Task<StringEntityResponse> Post(string url, object body)
    {
        var response = await Client.PostAsJsonAsync(url, body, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StringEntityResponse>(Ct))!;
    }

    protected async Task Put(string url, object body)
    {
        var response = await Client.PutAsJsonAsync(url, body, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    protected async Task<JsonElement> Get(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

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
        var entity = await db.Set<StringEntity>().FindAsync([id], Ct);
        Assert.NotNull(entity);
        Assert.Equal(expectedValue, entity!.Value.Value);
        Assert.Equal(expectedNullableValue, entity.NullableValue?.Value);
    }

    public void Dispose()
    {
        _scope.Dispose();
        Client.Dispose();
    }
}
