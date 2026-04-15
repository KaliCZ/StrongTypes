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

        await AssertStringEntity(SqlDb, id, "Alice", "Alice's nullable value");
        await AssertStringEntity(PgDb, id, "Alice", "Alice's nullable value");
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

        await AssertStringEntity(SqlDb, id, "Bob", "Bob's nullable value");
        await AssertStringEntity(PgDb, id, "Bob", "Bob's nullable value");
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

        await AssertStringEntity(SqlDb, id, "Carol", null);
        await AssertStringEntity(PgDb, id, "Carol", null);
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
