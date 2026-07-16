using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;

namespace StrongTypes.Api.Controllers;

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
