using StrongTypes.Api.Models;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStringEntityTests(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/non-nullable",
            new { Value = "Original", NullableValue = "Original nullable value" },
            ct);

        await Client.PutJsonAsync(
            $"/string-entities/{created.Id}/non-nullable",
            new { Value = "Updated", NullableValue = "Updated nullable value" },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Updated", "Updated nullable value");
        await AssertStringEntity(PgDb, created.Id, "Updated", "Updated nullable value");
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/nullable",
            new { Value = "Dave", NullableValue = (string?)null },
            ct);

        await Client.PutJsonAsync(
            $"/string-entities/{created.Id}/nullable",
            new { Value = "Dave", NullableValue = "Now has a nullable value" },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Dave", "Now has a nullable value");
        await AssertStringEntity(PgDb, created.Id, "Dave", "Now has a nullable value");
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await Client.PostJsonAsync<StringEntityResponse>(
            "/string-entities/non-nullable",
            new { Value = "Eve", NullableValue = "Eve's nullable value" },
            ct);

        await Client.PutJsonAsync(
            $"/string-entities/{created.Id}/nullable",
            new { Value = "Eve", NullableValue = (string?)null },
            ct);

        await AssertStringEntity(SqlDb, created.Id, "Eve", null);
        await AssertStringEntity(PgDb, created.Id, "Eve", null);
    }
}
