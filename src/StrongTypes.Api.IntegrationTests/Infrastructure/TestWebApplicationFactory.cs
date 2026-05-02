using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Shared fixture that boots both database containers once per test collection,
/// overrides the connection strings via configuration (so Program.cs's DbContext
/// registrations pick them up unchanged), and creates the schema via EnsureCreated.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder().Build();
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder().Build();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Start both containers in parallel; containers must be up before the
        // host is built so that ConfigureAppConfiguration can read their connection strings.
        await Task.WhenAll(_sqlContainer.StartAsync(), _pgContainer.StartAsync());

        // Accessing Services triggers the lazy host build.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<SqlServerDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PostgreSqlDbContext>().Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = _sqlContainer.GetConnectionString(),
                ["ConnectionStrings:PostgreSql"] = _pgContainer.GetConnectionString(),
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }
}
