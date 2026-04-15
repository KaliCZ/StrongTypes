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
/// Generic base for any <see cref="IEntity{T}"/>. Supplies Client, both
/// DbContexts, the current CancellationToken, the standard write/read routes
/// derived from <see cref="RoutePrefix"/>, HTTP wrappers that bake in the
/// cancellation token and the <see cref="EntityResponse"/> shape, a
/// <see cref="Body{TValue}"/> helper for wire bodies, and the generic
/// <see cref="AssertEntity"/> helper that reads from a <see cref="DbSet{TEntity}"/>.
/// </summary>
public abstract class IntegrationTestBase<TEntity, T>(TestWebApplicationFactory factory) : IDisposable
    where TEntity : class, IEntity<T>
    where T : class
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    protected DbSet<TEntity> SqlSet => SqlDb.Set<TEntity>();
    protected DbSet<TEntity> PgSet => PgDb.Set<TEntity>();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Route segment this entity is exposed under, e.g. "non-empty-string-entities".</summary>
    protected abstract string RoutePrefix { get; }

    protected string NonNullable => $"/{RoutePrefix}/non-nullable";
    protected string Nullable => $"/{RoutePrefix}/nullable";
    protected string UpdateNonNullable(Guid id) => $"/{RoutePrefix}/{id}/non-nullable";
    protected string UpdateNullable(Guid id) => $"/{RoutePrefix}/{id}/nullable";
    protected string SqlServerGet(Guid id) => $"/{RoutePrefix}/{id}/sql-server";
    protected string PostgreSqlGet(Guid id) => $"/{RoutePrefix}/{id}/postgresql";

    /// <summary>
    /// Builds the { Value, NullableValue } request body used by every write endpoint.
    /// Generic over the wire type so tests can send plain strings (or any other scalar)
    /// regardless of the strong type the server binds them into.
    /// </summary>
    protected static object Body<TValue>(TValue value, TValue? nullableValue) =>
        new { Value = value, NullableValue = nullableValue };

    protected async Task<EntityResponse> Post(string url, object body)
    {
        var response = await Client.PostAsJsonAsync(url, body, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EntityResponse>(Ct))!;
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
    /// Fetches the entity with the given id from the supplied DbSet and asserts
    /// that its Value and NullableValue match the expected values.
    /// </summary>
    protected static async Task AssertEntity(
        DbSet<TEntity> set,
        Guid id,
        T expectedValue,
        T? expectedNullableValue)
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
