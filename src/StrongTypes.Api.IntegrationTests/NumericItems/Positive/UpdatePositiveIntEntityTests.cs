using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Positive;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdatePositiveIntEntityTests(TestWebApplicationFactory factory)
    : PositiveIntEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(5, 42));
        await Put(UpdateNonNullable(created.Id), Body(100, 200));

        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(100), Positive<int>.Create(200));
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(100), Positive<int>.Create(200));
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(Nullable, Body(5, (int?)null));
        await Put(UpdateNullable(created.Id), Body(5, (int?)42));

        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(5), Positive<int>.Create(42));
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(5), Positive<int>.Create(42));
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(NonNullable, Body(5, 42));
        await Put(UpdateNullable(created.Id), Body(5, (int?)null));

        await AssertEntity(SqlSet, created.Id, Positive<int>.Create(5), null);
        await AssertEntity(PgSet, created.Id, Positive<int>.Create(5), null);
    }
}
