using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetStringEntityTests(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;
        var entity = new StringEntity("Alice", "Alice's nullable value");
        SqlDb.StringEntities.Add(entity);
        PgDb.StringEntities.Add(entity);
        await SqlDb.SaveChangesAsync(ct);
        await PgDb.SaveChangesAsync(ct);

        var sqlResponse = await Client.GetAsync($"/string-entities/{entity.Id}/sql-server", ct);
        Assert.Equal(HttpStatusCode.OK, sqlResponse.StatusCode);
        var sqlJson = await sqlResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", sqlJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", sqlJson.GetProperty("nullableValue").GetString());

        var pgResponse = await Client.GetAsync($"/string-entities/{entity.Id}/postgresql", ct);
        Assert.Equal(HttpStatusCode.OK, pgResponse.StatusCode);
        var pgJson = await pgResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", pgJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", pgJson.GetProperty("nullableValue").GetString());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;
        var entity = new StringEntity("Carol", null);
        SqlDb.StringEntities.Add(entity);
        PgDb.StringEntities.Add(entity);
        await SqlDb.SaveChangesAsync(ct);
        await PgDb.SaveChangesAsync(ct);

        var sqlJson = await Client.GetFromJsonAsync<JsonElement>(
            $"/string-entities/{entity.Id}/sql-server", ct);
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", sqlJson.GetProperty("value").GetString());

        var pgJson = await Client.GetFromJsonAsync<JsonElement>(
            $"/string-entities/{entity.Id}/postgresql", ct);
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", pgJson.GetProperty("value").GetString());
    }
}
