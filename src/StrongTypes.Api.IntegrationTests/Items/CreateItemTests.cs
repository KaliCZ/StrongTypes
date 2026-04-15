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
        var created = await Post(NonNullable, Body("Alice", "Alice's nullable value"));
        await AssertStringEntity(SqlDb, created.Id, "Alice", "Alice's nullable value");
        await AssertStringEntity(PgDb, created.Id, "Alice", "Alice's nullable value");
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body("Bob", "Bob's nullable value"));
        await AssertStringEntity(SqlDb, created.Id, "Bob", "Bob's nullable value");
        await AssertStringEntity(PgDb, created.Id, "Bob", "Bob's nullable value");
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body("Carol", (string?)null));
        await AssertStringEntity(SqlDb, created.Id, "Carol", null);
        await AssertStringEntity(PgDb, created.Id, "Carol", null);
    }

    [Fact]
    public async Task NonNullable_BothValuesNull_ReturnsValidationError()
    {
        // [ApiController] + non-nullable reference type properties = implicit [Required]
        // → ASP.NET Core returns 400 with a ValidationProblemDetails body.
        var response = await Client.PostAsJsonAsync(NonNullable, Body<string?>(null, null), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.True(json.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.EnumerateObject().Any(), "Expected at least one validation error");
    }
}
