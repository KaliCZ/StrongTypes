using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Endpoints;

public static class StringEntityEndpoints
{
    public static void MapStringEntityEndpoints(this WebApplication app)
    {
        app.MapPost("/string-entities/non-nullable", CreateNonNullable);
        app.MapPost("/string-entities/nullable", CreateNullable);
        app.MapPut("/string-entities/{id:guid}/non-nullable", UpdateNonNullable);
        app.MapPut("/string-entities/{id:guid}/nullable", UpdateNullable);
    }

    private static async Task<IResult> CreateNonNullable(
        CreateNonNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var id = Guid.NewGuid();

        sqlCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        pgCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Created($"/string-entities/{id}", new StringEntityResponse(id));
    }

    private static async Task<IResult> CreateNullable(
        CreateNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var id = Guid.NewGuid();

        sqlCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        pgCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Created($"/string-entities/{id}", new StringEntityResponse(id));
    }

    private static async Task<IResult> UpdateNonNullable(
        Guid id,
        UpdateNonNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var sqlEntity = await sqlCtx.StringEntities.FindAsync(id);
        var pgEntity = await pgCtx.StringEntities.FindAsync(id);

        if (sqlEntity is null || pgEntity is null)
            return Results.NotFound();

        sqlEntity.Value = request.Value;
        sqlEntity.NullableValue = request.NullableValue;
        pgEntity.Value = request.Value;
        pgEntity.NullableValue = request.NullableValue;

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Ok(new StringEntityResponse(id));
    }

    private static async Task<IResult> UpdateNullable(
        Guid id,
        UpdateNullableRequest request,
        SqlServerDbContext sqlCtx,
        PostgreSqlDbContext pgCtx)
    {
        var sqlEntity = await sqlCtx.StringEntities.FindAsync(id);
        var pgEntity = await pgCtx.StringEntities.FindAsync(id);

        if (sqlEntity is null || pgEntity is null)
            return Results.NotFound();

        sqlEntity.Value = request.Value;
        sqlEntity.NullableValue = request.NullableValue;
        pgEntity.Value = request.Value;
        pgEntity.NullableValue = request.NullableValue;

        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();

        return Results.Ok(new StringEntityResponse(id));
    }
}

public record CreateNonNullableRequest(string Value, string NullableValue);
public record CreateNullableRequest(string Value, string? NullableValue);
public record UpdateNonNullableRequest(string Value, string NullableValue);
public record UpdateNullableRequest(string Value, string? NullableValue);
public record StringEntityResponse(Guid Id);
