using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStringEntityTests(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var createResponse = await Client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Original", NullableValue = "Original nullable value" },
            ct);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await Client.PutAsJsonAsync(
            $"/string-entities/{id}/non-nullable",
            new { Value = "Updated", NullableValue = "Updated nullable value" },
            ct);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        await AssertStringEntity(SqlDb, id, "Updated", "Updated nullable value");
        await AssertStringEntity(PgDb, id, "Updated", "Updated nullable value");
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var createResponse = await Client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Dave", NullableValue = (string?)null },
            ct);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await Client.PutAsJsonAsync(
            $"/string-entities/{id}/nullable",
            new { Value = "Dave", NullableValue = "Now has a nullable value" },
            ct);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        await AssertStringEntity(SqlDb, id, "Dave", "Now has a nullable value");
        await AssertStringEntity(PgDb, id, "Dave", "Now has a nullable value");
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var createResponse = await Client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Eve", NullableValue = "Eve's nullable value" },
            ct);
        var createJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createJson.GetProperty("id").GetGuid();

        var updateResponse = await Client.PutAsJsonAsync(
            $"/string-entities/{id}/nullable",
            new { Value = "Eve", NullableValue = (string?)null },
            ct);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        await AssertStringEntity(SqlDb, id, "Eve", null);
        await AssertStringEntity(PgDb, id, "Eve", null);
    }
}
