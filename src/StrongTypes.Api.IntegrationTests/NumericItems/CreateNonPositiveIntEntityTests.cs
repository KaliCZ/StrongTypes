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
    public async Task Nullable_WithNullNullableValue_PersistsNullInBothDatabases()
    {
        var created = await Post(Nullable, Body(-5, (int?)null));
        await AssertEntity(SqlSet, created.Id, NonPositive<int>.Create(-5), null);
        await AssertEntity(PgSet, created.Id, NonPositive<int>.Create(-5), null);
    }
}
