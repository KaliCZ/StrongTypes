using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Models;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateStringEntityTests
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateStringEntityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonNullable_UpdatesValueAndNullableValueInBothDatabases()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new CreateNonNullableRequest("Original", "Original nullable value"));
        var created = await createResponse.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{created!.Id}/non-nullable",
            new UpdateNonNullableRequest("Updated", "Updated nullable value"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(created.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(created.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Updated", sqlEntity!.Value);
        Assert.Equal("Updated nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Updated", pgEntity!.Value);
        Assert.Equal("Updated nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/nullable",
            new CreateNullableRequest("Dave", null));
        var created = await createResponse.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{created!.Id}/nullable",
            new UpdateNullableRequest("Dave", "Now has a nullable value"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(created.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(created.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Dave", sqlEntity!.Value);
        Assert.Equal("Now has a nullable value", sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Dave", pgEntity!.Value);
        Assert.Equal("Now has a nullable value", pgEntity.NullableValue);
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/string-entities/non-nullable",
            new CreateNonNullableRequest("Eve", "Eve's nullable value"));
        var created = await createResponse.Content.ReadFromJsonAsync<StringEntityResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/string-entities/{created!.Id}/nullable",
            new UpdateNullableRequest("Eve", null));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlEntity = await sp.GetRequiredService<SqlServerDbContext>().StringEntities.FindAsync(created.Id);
        var pgEntity = await sp.GetRequiredService<PostgreSqlDbContext>().StringEntities.FindAsync(created.Id);

        Assert.NotNull(sqlEntity);
        Assert.Equal("Eve", sqlEntity!.Value);
        Assert.Null(sqlEntity.NullableValue);

        Assert.NotNull(pgEntity);
        Assert.Equal("Eve", pgEntity!.Value);
        Assert.Null(pgEntity.NullableValue);
    }
}
