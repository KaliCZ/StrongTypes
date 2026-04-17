using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Infrastructure;

namespace StrongTypes.Api.Data;

public class PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : DbContext(options)
{
    public DbSet<NonEmptyStringEntity> NonEmptyStringEntities { get; set; }
    public DbSet<PositiveIntEntity> PositiveIntEntities { get; set; }
    public DbSet<NonNegativeIntEntity> NonNegativeIntEntities { get; set; }
    public DbSet<NegativeIntEntity> NegativeIntEntities { get; set; }
    public DbSet<NonPositiveIntEntity> NonPositiveIntEntities { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseStrongTypes();
}
