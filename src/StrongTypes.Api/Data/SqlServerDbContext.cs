using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Data;

public class SqlServerDbContext : DbContext
{
    public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : base(options) { }

    public DbSet<StringEntity> StringEntities { get; set; }
}
