using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateStringEntityTests(TestWebApplicationFactory factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Alice", NullableValue = "Alice's nullable value" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

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
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Bob", NullableValue = "Bob's nullable value" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

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
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Carol", NullableValue = (string?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Carol", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Carol", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }
}
