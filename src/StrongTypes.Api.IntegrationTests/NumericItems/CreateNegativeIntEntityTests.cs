using System.Net;
using System.Net.Http.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NegativeIntEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(-5, -42));
        await AssertEntity(SqlSet, created.Id, Negative<int>.Create(-5), Negative<int>.Create(-42));
        await AssertEntity(PgSet, created.Id, Negative<int>.Create(-5), Negative<int>.Create(-42));
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body(-10, (int?)-7));
        await AssertEntity(SqlSet, created.Id, Negative<int>.Create(-10), Negative<int>.Create(-7));
        await AssertEntity(PgSet, created.Id, Negative<int>.Create(-10), Negative<int>.Create(-7));
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body(-3, (int?)null));
        await AssertEntity(SqlSet, created.Id, Negative<int>.Create(-3), null);
        await AssertEntity(PgSet, created.Id, Negative<int>.Create(-3), null);
    }

    [Fact]
    public async Task NonNullable_InvalidValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body(0, 0), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
