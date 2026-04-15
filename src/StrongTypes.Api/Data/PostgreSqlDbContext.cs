using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Infrastructure;

namespace StrongTypes.Api.Data;

public class PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : DbContext(options)
{
    public DbSet<NonEmptyStringEntity> NonEmptyStringEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<NonEmptyStringEntity>();
        entity.Property(e => e.Value).HasConversion<NonEmptyStringValueConverter>();
        entity.Property(e => e.NullableValue).HasConversion<NonEmptyStringValueConverter>();
    }
}
