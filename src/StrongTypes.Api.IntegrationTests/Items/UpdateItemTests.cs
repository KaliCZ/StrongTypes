using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStringEntityTests(TestWebApplicationFactory factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Original", NullableValue = "Original nullable value" });
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{id}/non-nullable",
            new { Value = "Updated", NullableValue = "Updated nullable value" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Updated", sqlEntity!.Value);
        Assert.Equal("Updated nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Updated", pgEntity!.Value);
        Assert.Equal("Updated nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Dave", NullableValue = (string?)null });
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{id}/nullable",
            new { Value = "Dave", NullableValue = "Now has a nullable value" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Dave", sqlEntity!.Value);
        Assert.Equal("Now has a nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Dave", pgEntity!.Value);
        Assert.Equal("Now has a nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Eve", NullableValue = "Eve's nullable value" });
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{id}/nullable",
            new { Value = "Eve", NullableValue = (string?)null });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Eve", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Eve", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }
}
