using System.Net;
using System.Net.Http.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateNonNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NonNegativeIntEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(0, 42));
        await AssertEntity(SqlSet, created.Id, NonNegative<int>.Create(0), NonNegative<int>.Create(42));
        await AssertEntity(PgSet, created.Id, NonNegative<int>.Create(0), NonNegative<int>.Create(42));
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body(10, (int?)7));
        await AssertEntity(SqlSet, created.Id, NonNegative<int>.Create(10), NonNegative<int>.Create(7));
        await AssertEntity(PgSet, created.Id, NonNegative<int>.Create(10), NonNegative<int>.Create(7));
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body(5, (int?)null));
        await AssertEntity(SqlSet, created.Id, NonNegative<int>.Create(5), null);
        await AssertEntity(PgSet, created.Id, NonNegative<int>.Create(5), null);
    }

    [Fact]
    public async Task NonNullable_NegativeValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body(-1, 0), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
