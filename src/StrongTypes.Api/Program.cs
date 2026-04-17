using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;
using StrongTypes.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<SqlServerDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"))
    .UseStrongTypes());
builder.Services.AddDbContext<PostgreSqlDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"))
    .UseStrongTypes());

var app = builder.Build();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
