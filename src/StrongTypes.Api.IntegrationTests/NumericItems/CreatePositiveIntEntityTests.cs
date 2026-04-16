using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

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
}
