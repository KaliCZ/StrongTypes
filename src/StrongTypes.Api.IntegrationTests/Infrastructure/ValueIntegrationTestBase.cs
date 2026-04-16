using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Value-type counterpart of <see cref="IntegrationTestBase{TEntity, T}"/>.
/// Same shape — Client, both DbContexts, route helpers, HTTP wrappers — but
/// for entities whose strong type <typeparamref name="T"/> is a struct.
/// </summary>
public abstract class ValueIntegrationTestBase<TEntity, T>(TestWebApplicationFactory factory) : IDisposable
    where TEntity : class, IValueEntity<TEntity, T>
    where T : struct
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();

    protected HttpClient Client { get; } = factory.CreateClient();

    protected SqlServerDbContext SqlDb => _scope.ServiceProvider.GetRequiredService<SqlServerDbContext>();
    protected PostgreSqlDbContext PgDb => _scope.ServiceProvider.GetRequiredService<PostgreSqlDbContext>();

    protected DbSet<TEntity> SqlSet => SqlDb.Set<TEntity>();
    protected DbSet<TEntity> PgSet => PgDb.Set<TEntity>();

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract string RoutePrefix { get; }

    protected string NonNullable => $"/{RoutePrefix}/non-nullable";
    protected string Nullable => $"/{RoutePrefix}/nullable";
    protected string UpdateNonNullable(Guid id) => $"/{RoutePrefix}/{id}/non-nullable";
    protected string UpdateNullable(Guid id) => $"/{RoutePrefix}/{id}/nullable";
    protected string SqlServerGet(Guid id) => $"/{RoutePrefix}/{id}/sql-server";
    protected string PostgreSqlGet(Guid id) => $"/{RoutePrefix}/{id}/postgresql";

    protected static object Body<TValue>(TValue value, TValue? nullableValue) where TValue : struct =>
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
