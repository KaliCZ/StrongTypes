using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Endpoints;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateStringEntityTests
{
    private readonly TestWebApplicationFactory _factory;

    public CreateStringEntityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new CreateNonNullableRequest("Alice", "Alice's nullable value"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(result!.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(result.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Alice", sqlEntity!.Value);
        Assert.Equal("Alice's nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Alice", pgEntity!.Value);
        Assert.Equal("Alice's nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new CreateNullableRequest("Bob", "Bob's nullable value"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(result!.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(result.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Bob", sqlEntity!.Value);
        Assert.Equal("Bob's nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Bob", pgEntity!.Value);
        Assert.Equal("Bob's nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new CreateNullableRequest("Carol", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(result!.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(result.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Carol", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Carol", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }
}
