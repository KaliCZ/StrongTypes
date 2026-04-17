using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonNegative;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetNonNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NonNegativeIntEntityTestBase(factory)
{
    [Fact]
    public async Task ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = NonNegativeIntEntity.Create(NonNegative<int>.Create(0), NonNegative<int>.Create(42));
        SqlDb.NonNegativeIntEntities.Add(entity);
        PgDb.NonNegativeIntEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal(0, sqlJson.GetProperty("value").GetInt32());
        Assert.Equal(42, sqlJson.GetProperty("nullableValue").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(0, pgJson.GetProperty("value").GetInt32());
        Assert.Equal(42, pgJson.GetProperty("nullableValue").GetInt32());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = NonNegativeIntEntity.Create(NonNegative<int>.Create(7), null);
        SqlDb.NonNegativeIntEntities.Add(entity);
        PgDb.NonNegativeIntEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(7, sqlJson.GetProperty("value").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(7, pgJson.GetProperty("value").GetInt32());
    }
}
