using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetNonEmptyStringEntityTests(TestWebApplicationFactory factory)
    : NonEmptyStringEntityTestBase(factory)
{
    [Fact]
    public async Task ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = NonEmptyStringEntity.Create(NonEmptyString.Create("Alice"), NonEmptyString.Create("Alice's nullable value"));
        SqlDb.NonEmptyStringEntities.Add(entity);
        PgDb.NonEmptyStringEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", sqlJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", sqlJson.GetProperty("nullableValue").GetString());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal("Alice", pgJson.GetProperty("value").GetString());
        Assert.Equal("Alice's nullable value", pgJson.GetProperty("nullableValue").GetString());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = NonEmptyStringEntity.Create(NonEmptyString.Create("Carol"), null);
        SqlDb.NonEmptyStringEntities.Add(entity);
        PgDb.NonEmptyStringEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", sqlJson.GetProperty("value").GetString());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal("Carol", pgJson.GetProperty("value").GetString());
    }
}
