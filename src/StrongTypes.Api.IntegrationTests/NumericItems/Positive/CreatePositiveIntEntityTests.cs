using System.Net;
using System.Net.Http.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Positive;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreatePositiveIntEntityTests(TestWebApplicationFactory factory)
    : PositiveIntEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(5, 42));
        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(5), Positive<int>.Create(42));
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(5), Positive<int>.Create(42));
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body(10, (int?)7));
        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(10), Positive<int>.Create(7));
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(10), Positive<int>.Create(7));
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body(3, (int?)null));
        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(3), null);
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(3), null);
    }

    // Exercises every rejection path: the invariant must fail whether the
    // invalid number is on Value or on NullableValue, and on both endpoints.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task NonNullable_InvalidValues_ReturnsBadRequest(int value, int nullableValue)
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body(value, nullableValue), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task Nullable_InvalidValues_ReturnsBadRequest(int value, int? nullableValue)
    {
        var response = await Client.PostAsJsonAsync(Nullable, Body(value, nullableValue), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
