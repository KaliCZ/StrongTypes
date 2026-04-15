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

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

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

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

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

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Eve", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Eve", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }
}
