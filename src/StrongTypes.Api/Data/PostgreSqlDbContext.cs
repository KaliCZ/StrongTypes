using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Data;

public class PostgreSqlDbContext : DbContext
{
    public PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : base(options) { }

    public DbSet<StringEntity> StringEntities { get; set; }
}
