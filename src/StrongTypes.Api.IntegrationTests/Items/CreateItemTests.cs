using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateStringEntityTests(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = "Alice", NullableValue = "Alice's nullable value" },
            ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = json.GetProperty("id").GetGuid();

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

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
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Bob", NullableValue = "Bob's nullable value" },
            ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = json.GetProperty("id").GetGuid();

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

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
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.PostAsJsonAsync(
            "/string-entities/nullable",
            new { Value = "Carol", NullableValue = (string?)null },
            ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = json.GetProperty("id").GetGuid();

        var sqlEntity = await SqlDb.StringEntities.FindAsync([id], ct);
        var pgEntity = await PgDb.StringEntities.FindAsync([id], ct);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Carol", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Carol", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }

    [Fact]
    public async Task NonNullable_BothValuesNull_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new { Value = (string?)null, NullableValue = (string?)null },
            ct);

        // [ApiController] + non-nullable reference type properties = implicit [Required]
        // → ASP.NET Core returns 400 with a ValidationProblemDetails body.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(json.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.EnumerateObject().Any(), "Expected at least one validation error");
    }
}
