using Microsoft.EntityFrameworkCore;
using StrongTypes;
using StrongTypes.Api.Data;
using StrongTypes.EfCore;

var builder = WebApplication.CreateBuilder(args);

// Register MaybeJsonConverterFactory globally so Maybe<T>? PATCH properties
// distinguish absent/null/value. The [JsonConverter] attribute on Maybe<T>
// covers Maybe<T> itself, but STJ's built-in Nullable<T> handling wraps the
// inner converter in a way that collapses JSON null into a null nullable —
// the factory needs to intercept Nullable<Maybe<T>> via Options.Converters.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new MaybeJsonConverterFactory()));
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
