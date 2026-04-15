using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Data;

public class PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> options) : DbContext(options)
{
    public DbSet<StringEntity> StringEntities { get; set; }
}
