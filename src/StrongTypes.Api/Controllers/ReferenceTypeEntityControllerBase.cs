using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Controller base for entities whose <typeparamref name="T"/> is a reference
/// type. <c>TNullable</c> is fixed to <c>T?</c> (the same reference annotated
/// nullable), so PATCH conversions are direct null-checks with no boxing.
/// </summary>
public abstract class ReferenceTypeEntityControllerBase<TEntity, T>(
    SqlServerDbContext sqlCtx,
    PostgreSqlDbContext pgCtx) : EntityControllerBase<TEntity>(sqlCtx, pgCtx)
    where TEntity : class, IEntity<TEntity, T, T?>
    where T : class
{
    [HttpGet("{id:guid}/sql-server")]
    public async Task<IActionResult> GetFromSqlServer(Guid id)
    {
        var entity = await SqlSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(ReferenceEntityDto<T>.From(entity));
    }

    [HttpGet("{id:guid}/postgresql")]
    public async Task<IActionResult> GetFromPostgreSql(Guid id)
    {
        var entity = await PgSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(ReferenceEntityDto<T>.From(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReferenceEntityRequest<T> request)
    {
        var entity = TEntity.Create(request.Value, request.NullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SaveAllAsync();
        return Created($"{Request.Path}/{entity.Id}", new EntityResponse(entity.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ReferenceEntityRequest<T> request)
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
    public async Task<IActionResult> Patch(Guid id, ReferenceEntityPatchRequest<T> request)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null) return NotFound();

        if (request.Value is not null)
        {
            sqlEntity.Value = request.Value;
            pgEntity.Value = request.Value;
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
