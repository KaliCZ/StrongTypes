using System.Net;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateNonPositiveIntEntityTests(TestWebApplicationFactory factory)
    : NonPositiveIntEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_PersistsValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(0, -42));
        await AssertEntity(SqlSet, created.Id, NonPositive<int>.Create(0), NonPositive<int>.Create(-42));
        await AssertEntity(PgSet, created.Id, NonPositive<int>.Create(0), NonPositive<int>.Create(-42));
    }

    [Fact]
    public async Task Nullable_WithNullableValue_PersistsBothValuesInBothDatabases()
    {
        var created = await Post(Nullable, Body(-10, (int?)-7));
        await AssertEntity(SqlSet, created.Id, NonPositive<int>.Create(-10), NonPositive<int>.Create(-7));
        await AssertEntity(PgSet, created.Id, NonPositive<int>.Create(-10), NonPositive<int>.Create(-7));
    }

    [Fact]
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body(-5, (int?)null));
        await AssertEntity(SqlSet, created.Id, NonPositive<int>.Create(-5), null);
        await AssertEntity(PgSet, created.Id, NonPositive<int>.Create(-5), null);
    }

    [Fact]
    public async Task NonNullable_InvalidValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(NonNullable, Body(1, 1), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
