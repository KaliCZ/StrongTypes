using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Negative;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NegativeIntEntityTestBase(factory)
{
    [Fact]
    public async Task ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = NegativeIntEntity.Create(Negative<int>.Create(-5), Negative<int>.Create(-42));
        SqlDb.NegativeIntEntities.Add(entity);
        PgDb.NegativeIntEntities.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal(-5, sqlJson.GetProperty("value").GetInt32());
        Assert.Equal(-42, sqlJson.GetProperty("nullableValue").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(-5, pgJson.GetProperty("value").GetInt32());
        Assert.Equal(-42, pgJson.GetProperty("nullableValue").GetInt32());
    }

    [Fact]
    public async Task SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = NegativeIntEntity.Create(Negative<int>.Create(-1), null);
        SqlDb.NegativeIntEntities.Add(entity);
        PgDb.NegativeIntEntities.Add(entity);
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
