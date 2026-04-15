using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Infrastructure;

namespace StrongTypes.Api.Data;

public class PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : DbContext(options)
{
    public DbSet<StringEntity> StringEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StringEntity>();
        entity.Property(e => e.Value).HasConversion<NonEmptyStringValueConverter>();
        entity.Property(e => e.NullableValue).HasConversion<NonEmptyStringValueConverter>();
    }
}
