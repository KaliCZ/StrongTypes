using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.Api.Endpoints;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Items;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateItemTests
{
    private readonly TestWebApplicationFactory _factory;

    public CreateItemTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonNullable_PersistsNameAndDescriptionInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/items/non-nullable",
            new CreateNonNullableRequest("Alice", "Alice's description"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(result!.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(result.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Alice", sqlItem!.Name);
        Assert.Equal("Alice's description", sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Alice", pgItem!.Name);
        Assert.Equal("Alice's description", pgItem.Description);
    }

    [Fact]
    public async Task Nullable_WithDescription_PersistsNameAndDescriptionInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/items/nullable",
            new CreateNullableRequest("Bob", "Bob's description"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(result!.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(result.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Bob", sqlItem!.Name);
        Assert.Equal("Bob's description", sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Bob", pgItem!.Name);
        Assert.Equal("Bob's description", pgItem.Description);
    }

    [Fact]
    public async Task Nullable_WithNullDescription_PersistsNullDescriptionInBothDatabases()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/items/nullable",
            new CreateNullableRequest("Carol", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ItemResponse>();
        Assert.NotNull(result);

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var sqlItem = await sp.GetRequiredService<SqlServerDbContext>().Items.FindAsync(result!.Id);
        var pgItem = await sp.GetRequiredService<PostgreSqlDbContext>().Items.FindAsync(result.Id);

        Assert.NotNull(sqlItem);
        Assert.Equal("Carol", sqlItem!.Name);
        Assert.Null(sqlItem.Description);

        Assert.NotNull(pgItem);
        Assert.Equal("Carol", pgItem!.Name);
        Assert.Null(pgItem.Description);
    }
}
