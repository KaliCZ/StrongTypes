using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Endpoints;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateItemTests
{
    private readonly TestWebApplicationFactory _factory;

    public UpdateItemTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonNullable_UpdatesNameAndDescriptionInBothDatabases()
    {
        var client = _factory.CreateClient();

        // Seed via the API so both databases have the item
        var createResponse = await client.PostAsJsonAsync(
            "/items/non-nullable",
            new CreateNonNullableRequest("Original", "Original description"));
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/items/{created!.Id}/non-nullable",
            new UpdateNonNullableRequest("Updated", "Updated description"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(created.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(created.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Updated", sqlItem!.Name);
        Assert.Equal("Updated description", sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Updated", pgItem!.Name);
        Assert.Equal("Updated description", pgItem.Description);
    }

    [Fact]
    public async Task Nullable_SetsDescriptionFromNullToValueInBothDatabases()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/items/nullable",
            new CreateNullableRequest("Dave", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/items/{created!.Id}/nullable",
            new UpdateNullableRequest("Dave", "Now has a description"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(created.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(created.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Dave", sqlItem!.Name);
        Assert.Equal("Now has a description", sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Dave", pgItem!.Name);
        Assert.Equal("Now has a description", pgItem.Description);
    }

    [Fact]
    public async Task Nullable_ClearsDescriptionToNullInBothDatabases()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/items/non-nullable",
            new CreateNonNullableRequest("Eve", "Eve's description"));
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/items/{created!.Id}/nullable",
            new UpdateNullableRequest("Eve", null));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(created.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(created.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Eve", sqlItem!.Name);
        Assert.Null(sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Eve", pgItem!.Name);
        Assert.Null(pgItem.Description);
    }
}
