using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Controller base for entities whose <typeparamref name="T"/> is a value type.
/// <c>TNullable</c> is fixed to <c>T?</c> (i.e. <c>Nullable&lt;T&gt;</c>), which
/// lets every conversion in PATCH be a direct nullable-value access — no boxing
/// or runtime casts.
/// </summary>
public abstract class StructTypeEntityControllerBase<TEntity, T>(
    SqlServerDbContext sqlCtx,
    PostgreSqlDbContext pgCtx) : EntityControllerBase<TEntity>(sqlCtx, pgCtx)
    where TEntity : class, IEntity<TEntity, T, T?>
    where T : struct
{
    [HttpGet("{id:guid}/sql-server")]
    public async Task<IActionResult> GetFromSqlServer(Guid id)
    {
        var entity = await SqlSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(StructEntityDto<T>.From(entity));
    }

    [HttpGet("{id:guid}/postgresql")]
    public async Task<IActionResult> GetFromPostgreSql(Guid id)
    {
        var entity = await PgSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(StructEntityDto<T>.From(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create(StructEntityRequest<T> request)
    {
        var entity = TEntity.Create(request.Value, request.NullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SaveAllAsync();
        return Created($"{Request.Path}/{entity.Id}", new EntityResponse(entity.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, StructEntityRequest<T> request)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null) return NotFound();
        sqlEntity.Update(request.Value, request.NullableValue);
        pgEntity.Update(request.Value, request.NullableValue);
        await SaveAllAsync();
        return Ok(new EntityResponse(id));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, StructEntityPatchRequest<T> request)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null) return NotFound();

        if (request.Value is { } v)
        {
            sqlEntity.Value = v;
            pgEntity.Value = v;
        }

        if (request.NullableValue is { } nv)
        {
            sqlEntity.NullableValue = nv.Value;
            pgEntity.NullableValue = nv.Value;
        }

        await SaveAllAsync();
        return Ok(new EntityResponse(id));
    }
}
