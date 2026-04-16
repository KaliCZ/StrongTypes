using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateNonEmptyStringEntityTests(TestWebApplicationFactory factory)
    : NonEmptyStringEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body("Alice", "Alice's nullable value"));
        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Alice"), NonEmptyString.Create("Alice's nullable value"));
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Alice"), NonEmptyString.Create("Alice's nullable value"));
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body("Bob", "Bob's nullable value"));
        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Bob"), NonEmptyString.Create("Bob's nullable value"));
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Bob"), NonEmptyString.Create("Bob's nullable value"));
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body("Carol", null));
        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Carol"), null);
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Carol"), null);
    }

    [Fact]
    public async Task NonNullable_BothValuesNull_ReturnsValidationError()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body<string?>(null, null), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.True(json.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.EnumerateObject().Any(), "Expected at least one validation error");
    }

    [Fact]
    public async Task NonNullable_WhitespaceOnlyValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body("   ", "valid"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonNullable_EmptyStringValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body("", "valid"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nullable_WhitespaceOnlyValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(Nullable, Body("   ", "valid"), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
