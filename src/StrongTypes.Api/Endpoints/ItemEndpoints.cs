using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this WebApplication app)
    {
        app.MapPost("/items/non-nullable", CreateNonNullable);
        app.MapPost("/items/nullable", CreateNullable);
        app.MapPut("/items/{id:guid}/non-nullable", UpdateNonNullable);
        app.MapPut("/items/{id:guid}/nullable", UpdateNullable);
    }

    private static async Task<IResult> CreateNonNullable(
        CreateNonNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var id = Guid.NewGuid();

        sqlCtx.Items.Add(new Item { Id = id, Name = request.Name, Description = request.Description });
        pgCtx.Items.Add(new Item { Id = id, Name = request.Name, Description = request.Description });

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Created($"/items/{id}", new ItemResponse(id));
    }

    private static async Task<IResult> CreateNullable(
        CreateNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var id = Guid.NewGuid();

        sqlCtx.Items.Add(new Item { Id = id, Name = request.Name, Description = request.Description });
        pgCtx.Items.Add(new Item { Id = id, Name = request.Name, Description = request.Description });

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Created($"/items/{id}", new ItemResponse(id));
    }

    private static async Task<IResult> UpdateNonNullable(
        Guid id,
        UpdateNonNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var sqlItem = await sqlCtx.Items.FindAsync(id);
        var pgItem = await pgCtx.Items.FindAsync(id);

        if (sqlItem is null || pgItem is null)
            return Results.NotFound();

        sqlItem.Name = request.Name;
        sqlItem.Description = request.Description;
        pgItem.Name = request.Name;
        pgItem.Description = request.Description;

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Ok(new ItemResponse(id));
    }

    private static async Task<IResult> UpdateNullable(
        Guid id,
        UpdateNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var sqlItem = await sqlCtx.Items.FindAsync(id);
        var pgItem = await pgCtx.Items.FindAsync(id);

        if (sqlItem is null || pgItem is null)
            return Results.NotFound();

        sqlItem.Name = request.Name;
        sqlItem.Description = request.Description;
        pgItem.Name = request.Name;
        pgItem.Description = request.Description;

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Ok(new ItemResponse(id));
    }
}

public record CreateNonNullableRequest(string Name, string Description);
public record CreateNullableRequest(string Name, string? Description);
public record UpdateNonNullableRequest(string Name, string Description);
public record UpdateNullableRequest(string Name, string? Description);
public record ItemResponse(Guid Id);
