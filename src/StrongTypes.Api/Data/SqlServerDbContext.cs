using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

public class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : DbContext(options)
{
    public DbSet<NonEmptyStringEntity> NonEmptyStringEntities { get; set; }

    public DbSet<PositiveIntEntity> PositiveIntEntities { get; set; }
    public DbSet<NonNegativeIntEntity> NonNegativeIntEntities { get; set; }
    public DbSet<NegativeIntEntity> NegativeIntEntities { get; set; }
    public DbSet<NonPositiveIntEntity> NonPositiveIntEntities { get; set; }

    public DbSet<PositiveLongEntity> PositiveLongEntities { get; set; }
    public DbSet<NonNegativeLongEntity> NonNegativeLongEntities { get; set; }
    public DbSet<NegativeLongEntity> NegativeLongEntities { get; set; }
    public DbSet<NonPositiveLongEntity> NonPositiveLongEntities { get; set; }

    public DbSet<PositiveShortEntity> PositiveShortEntities { get; set; }
    public DbSet<NonNegativeShortEntity> NonNegativeShortEntities { get; set; }
    public DbSet<NegativeShortEntity> NegativeShortEntities { get; set; }
    public DbSet<NonPositiveShortEntity> NonPositiveShortEntities { get; set; }

    public DbSet<PositiveDecimalEntity> PositiveDecimalEntities { get; set; }
    public DbSet<NonNegativeDecimalEntity> NonNegativeDecimalEntities { get; set; }
    public DbSet<NegativeDecimalEntity> NegativeDecimalEntities { get; set; }
    public DbSet<NonPositiveDecimalEntity> NonPositiveDecimalEntities { get; set; }

    public DbSet<PositiveFloatEntity> PositiveFloatEntities { get; set; }
    public DbSet<NonNegativeFloatEntity> NonNegativeFloatEntities { get; set; }
    public DbSet<NegativeFloatEntity> NegativeFloatEntities { get; set; }
    public DbSet<NonPositiveFloatEntity> NonPositiveFloatEntities { get; set; }

    public DbSet<PositiveDoubleEntity> PositiveDoubleEntities { get; set; }
    public DbSet<NonNegativeDoubleEntity> NonNegativeDoubleEntities { get; set; }
    public DbSet<NegativeDoubleEntity> NegativeDoubleEntities { get; set; }
    public DbSet<NonPositiveDoubleEntity> NonPositiveDoubleEntities { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.UseStrongTypes();
}
