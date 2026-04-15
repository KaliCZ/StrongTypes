using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrongTypes.Api.Data;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Shared fixture that boots both database containers once per test collection,
/// overrides the DbContext registrations with Testcontainers connection strings,
/// and creates the schema via EnsureCreated.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder().Build();
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder().Build();

    // xUnit v3 IAsyncLifetime uses ValueTask
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Start both containers in parallel; containers must be up before the
        // host is built so that ConfigureWebHost can read their connection strings.
        await Task.WhenAll(_sqlContainer.StartAsync(), _pgContainer.StartAsync());

        // Accessing Services triggers the lazy host build, which calls ConfigureWebHost.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<SqlServerDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PostgreSqlDbContext>().Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace SqlServer context with one pointing at the container.
            services.RemoveAll<DbContextOptions<SqlServerDbContext>>();
            services.RemoveAll<SqlServerDbContext>();
            services.AddDbContext<SqlServerDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace PostgreSQL context with one pointing at the container.
            services.RemoveAll<DbContextOptions<PostgreSqlDbContext>>();
            services.RemoveAll<PostgreSqlDbContext>();
            services.AddDbContext<PostgreSqlDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));
        });
    }

    // Overrides WebApplicationFactory.DisposeAsync (ValueTask), which also satisfies
    // xUnit v3 IAsyncLifetime.DisposeAsync (same signature).
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }
}
