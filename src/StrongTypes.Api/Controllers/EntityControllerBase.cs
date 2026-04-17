using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Shared controller logic for any <see cref="IEntity{TSelf, T, TNullable}"/>:
/// writes to both SQL Server and PostgreSQL, reads from whichever the route
/// specifies. Concrete controllers just supply a class-level <c>[Route]</c> +
/// <c>[ApiController]</c>; construction goes through <c>TEntity.Create</c>.
/// </summary>
public abstract class EntityControllerBase<TEntity, T, TNullable>(
    SqlServerDbContext sqlCtx,
    PostgreSqlDbContext pgCtx) : ControllerBase
    where TEntity : class, IEntity<TEntity, T, TNullable>
    where T : notnull
{
    private DbSet<TEntity> SqlSet => sqlCtx.Set<TEntity>();
    private DbSet<TEntity> PgSet => pgCtx.Set<TEntity>();

    [HttpGet("{id:guid}/sql-server")]
    public Task<IActionResult> GetFromSqlServer(Guid id) => GetAsync(SqlSet, id);

    [HttpGet("{id:guid}/postgresql")]
    public Task<IActionResult> GetFromPostgreSql(Guid id) => GetAsync(PgSet, id);

    [HttpPost("non-nullable")]
    public Task<IActionResult> CreateNonNullable(NonNullableRequest<T> request) =>
        CreateAsync(request.Value, ToNullable(request.NullableValue));

    [HttpPost("nullable")]
    public Task<IActionResult> CreateNullable(NullableRequest<T, TNullable> request) =>
        CreateAsync(request.Value, request.NullableValue);

    [HttpPut("{id:guid}/non-nullable")]
    public Task<IActionResult> UpdateNonNullable(Guid id, NonNullableRequest<T> request) =>
        UpdateAsync(id, request.Value, ToNullable(request.NullableValue));

    [HttpPut("{id:guid}/nullable")]
    public Task<IActionResult> UpdateNullable(Guid id, NullableRequest<T, TNullable> request) =>
        UpdateAsync(id, request.Value, request.NullableValue);

    private async Task<IActionResult> GetAsync(DbSet<TEntity> set, Guid id)
    {
        var entity = await set.FindAsync(id);
        return entity is null ? NotFound() : Ok(EntityDto<T, TNullable>.From(entity));
    }

    private async Task<IActionResult> CreateAsync(T value, TNullable nullableValue)
    {
        var entity = TEntity.Create(value, nullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"{Request.Path}/{entity.Id}", new EntityResponse(entity.Id));
    }

    private async Task<IActionResult> UpdateAsync(Guid id, T value, TNullable nullableValue)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null)
            return NotFound();
        sqlEntity.Update(value, nullableValue);
        pgEntity.Update(value, nullableValue);
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Ok(new EntityResponse(id));
    }

    // Bridge from T to TNullable when TNullable is T? — works for both reference
    // types (identity cast) and value types (boxed T unboxes to Nullable<T>).
    private static TNullable ToNullable(T value) => (TNullable)(object)value!;
}
