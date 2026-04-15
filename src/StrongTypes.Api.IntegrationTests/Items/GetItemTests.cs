using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

        var createResponse = await Client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Alice", NullableValue = "Alice's nullable value" },
            ct);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createJson.GetProperty("id").GetGuid();

        var sqlResponse = await Client.GetAsync($"/string-entities/{id}/sql-server", ct);
        Assert.Equal(HttpStatusCode.OK, sqlResponse.StatusCode);
        var sqlJson = await sqlResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", sqlJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", sqlJson.GetProperty("nullableValue").GetString());

        var pgResponse = await Client.GetAsync($"/string-entities/{id}/postgresql", ct);
        Assert.Equal(HttpStatusCode.OK, pgResponse.StatusCode);
        var pgJson = await pgResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", pgJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", pgJson.GetProperty("nullableValue").GetString());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var createResponse = await Client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Carol", NullableValue = (string?)null },
            ct);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createJson.GetProperty("id").GetGuid();

        var sqlJson = await Client.GetFromJsonAsync<JsonElement>(
            $"/string-entities/{id}/sql-server", ct);
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", sqlJson.GetProperty("value").GetString());

        var pgJson = await Client.GetFromJsonAsync<JsonElement>(
            $"/string-entities/{id}/postgresql", ct);
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", pgJson.GetProperty("value").GetString());
    }

}
