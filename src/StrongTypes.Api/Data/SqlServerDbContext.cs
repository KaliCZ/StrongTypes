using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Data;

public class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : DbContext(options)
{
    public DbSet<StringEntity> StringEntities { get; set; }
}
