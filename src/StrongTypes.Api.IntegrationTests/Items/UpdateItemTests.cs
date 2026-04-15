using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateNonEmptyStringEntityTests(TestWebApplicationFactory factory)
    : NonEmptyStringEntityTestBase(factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body("Original", "Original nullable value"));
        await Put(UpdateNonNullable(created.Id), Body("Updated", "Updated nullable value"));

        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Updated"), NonEmptyString.Create("Updated nullable value"));
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Updated"), NonEmptyString.Create("Updated nullable value"));
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(Nullable, Body("Dave", null));
        await Put(UpdateNullable(created.Id), Body("Dave", "Now has a nullable value"));

        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Dave"), NonEmptyString.Create("Now has a nullable value"));
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Dave"), NonEmptyString.Create("Now has a nullable value"));
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(NonNullable, Body("Eve", "Eve's nullable value"));
        await Put(UpdateNullable(created.Id), Body("Eve", null));

        await AssertEntity(SqlSet, created.Id, NonEmptyString.Create("Eve"), null);
        await AssertEntity(PgSet, created.Id, NonEmptyString.Create("Eve"), null);
    }
}
