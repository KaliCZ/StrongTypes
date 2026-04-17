using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonPositive;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetNonPositiveIntEntityTests(TestWebApplicationFactory factory)
    : NonPositiveIntEntityTestBase(factory)
{
    [Fact]
    public async Task ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = NonPositiveIntEntity.Create(NonPositive<int>.Create(0), NonPositive<int>.Create(-42));
        SqlDb.NonPositiveIntEntities.Add(entity);
        PgDb.NonPositiveIntEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal(0, sqlJson.GetProperty("value").GetInt32());
        Assert.Equal(-42, sqlJson.GetProperty("nullableValue").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(0, pgJson.GetProperty("value").GetInt32());
        Assert.Equal(-42, pgJson.GetProperty("nullableValue").GetInt32());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = NonPositiveIntEntity.Create(NonPositive<int>.Create(-1), null);
        SqlDb.NonPositiveIntEntities.Add(entity);
        PgDb.NonPositiveIntEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(-1, sqlJson.GetProperty("value").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(-1, pgJson.GetProperty("value").GetInt32());
    }
}
