using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Shared infrastructure for the struct- and reference-typed controller bases:
/// the two <see cref="DbSet{TEntity}"/> accessors and a helper that flushes both
/// <see cref="DbContext"/>s. Concrete controllers do not derive from this
/// directly — they pick <see cref="StructTypeEntityControllerBase{TEntity, T}"/>
/// or <see cref="ReferenceTypeEntityControllerBase{TEntity, T}"/>.
/// </summary>
public abstract class EntityControllerBase<TEntity>(
    SqlServerDbContext sqlCtx,
    PostgreSqlDbContext pgCtx) : ControllerBase
    where TEntity : class
{
    protected DbSet<TEntity> SqlSet => sqlCtx.Set<TEntity>();
    protected DbSet<TEntity> PgSet => pgCtx.Set<TEntity>();

    protected async Task SaveAllAsync()
    {
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
    }
}
