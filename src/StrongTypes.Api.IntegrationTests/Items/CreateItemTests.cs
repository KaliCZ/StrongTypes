using System.Net;
using System.Text.Json;
using StrongTypes.Api.Models;
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

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/non-nullable",
            new { Value = "Alice", NullableValue = "Alice's nullable value" },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Alice", "Alice's nullable value");
        await AssertStringEntity(PgDb, created.Id, "Alice", "Alice's nullable value");
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/nullable",
            new { Value = "Bob", NullableValue = "Bob's nullable value" },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Bob", "Bob's nullable value");
        await AssertStringEntity(PgDb, created.Id, "Bob", "Bob's nullable value");
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/nullable",
            new { Value = "Carol", NullableValue = (string?)null },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Carol", null);
        await AssertStringEntity(PgDb, created.Id, "Carol", null);
    }

    [Fact]
    public async Task NonNullable_BothValuesNull_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;

        // [ApiController] + non-nullable reference type properties = implicit [Required]
        // → ASP.NET Core returns 400 with a ValidationProblemDetails body.
        var json = await Client.PostJsonAsync<JsonElement>(
            "/string-entities/non-nullable",
            new { Value = (string?)null, NullableValue = (string?)null },
            ct,
            HttpStatusCode.BadRequest);

        Assert.True(json.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.EnumerateObject().Any(), "Expected at least one validation error");
    }
}
