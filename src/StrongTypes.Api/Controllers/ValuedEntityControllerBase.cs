using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Shared controller logic for any <see cref="IValuedEntity{T}"/>: writes to
/// both SQL Server and PostgreSQL, reads from whichever the route specifies.
/// Concrete controllers supply the entity factory and a class-level
/// <c>[Route]</c> + <c>[ApiController]</c>.
/// </summary>
public abstract class ValuedEntityControllerBase<TEntity, T>(
    SqlServerDbContext sqlCtx,
    PostgreSqlDbContext pgCtx) : ControllerBase
    where TEntity : class, IValuedEntity<T>
    where T : class
{
    protected abstract TEntity CreateEntity(T value, T? nullableValue);

    private DbSet<TEntity> SqlSet => sqlCtx.Set<TEntity>();
    private DbSet<TEntity> PgSet => pgCtx.Set<TEntity>();

    [HttpGet("{id:guid}/sql-server")]
    public Task<IActionResult> GetFromSqlServer(Guid id) => GetAsync(SqlSet, id);

    [HttpGet("{id:guid}/postgresql")]
    public Task<IActionResult> GetFromPostgreSql(Guid id) => GetAsync(PgSet, id);

    [HttpPost("non-nullable")]
    public Task<IActionResult> CreateNonNullable(NonNullableValuedRequest<T> request) =>
        CreateAsync(request.Value, request.NullableValue);

    [HttpPost("nullable")]
    public Task<IActionResult> CreateNullable(NullableValuedRequest<T> request) =>
        CreateAsync(request.Value, request.NullableValue);

    [HttpPut("{id:guid}/non-nullable")]
    public Task<IActionResult> UpdateNonNullable(Guid id, NonNullableValuedRequest<T> request) =>
        UpdateAsync(id, request.Value, request.NullableValue);

    [HttpPut("{id:guid}/nullable")]
    public Task<IActionResult> UpdateNullable(Guid id, NullableValuedRequest<T> request) =>
        UpdateAsync(id, request.Value, request.NullableValue);

    private async Task<IActionResult> GetAsync(DbSet<TEntity> set, Guid id)
    {
        var entity = await set.FindAsync(id);
        return entity is null ? NotFound() : Ok(ValuedEntityDto<T>.From(entity));
    }

    private async Task<IActionResult> CreateAsync(T value, T? nullableValue)
    {
        var entity = CreateEntity(value, nullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"{Request.Path}/{entity.Id}", new EntityResponse(entity.Id));
    }

    private async Task<IActionResult> UpdateAsync(Guid id, T value, T? nullableValue)
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
}
