using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStringEntityTests(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body("Original", "Original nullable value"));
        await Put(UpdateNonNullable(created.Id), Body("Updated", "Updated nullable value"));

        await AssertStringEntity(SqlDb, created.Id, "Updated", "Updated nullable value");
        await AssertStringEntity(PgDb, created.Id, "Updated", "Updated nullable value");
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(Nullable, Body("Dave", (string?)null));
        await Put(UpdateNullable(created.Id), Body("Dave", "Now has a nullable value"));

        await AssertStringEntity(SqlDb, created.Id, "Dave", "Now has a nullable value");
        await AssertStringEntity(PgDb, created.Id, "Dave", "Now has a nullable value");
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(NonNullable, Body("Eve", "Eve's nullable value"));
        await Put(UpdateNullable(created.Id), Body("Eve", (string?)null));

        await AssertStringEntity(SqlDb, created.Id, "Eve", null);
        await AssertStringEntity(PgDb, created.Id, "Eve", null);
    }
}
