using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongTypes.Api.Data;
using StrongTypes.EfCore;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Overrides connection strings via configuration so Program.cs's DbContext registrations are
/// exercised unchanged. SQL Server skipping rules live in testing.md.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DockerGroupLabel = "com.docker.compose.project";
    private const string DockerGroupName = "StrongTypes";

    private const string SkipSqlServerEnvVar = "STRONGTYPES_SKIP_SQLSERVER";

    private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromSeconds(45);

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    /// <summary>
    /// False only when the host opted into skipping SQL Server — gate every SQL-Server-only
    /// assertion on it; the stand-in in-memory stub does not exercise the real wire path.
    /// </summary>
    public bool SqlServerAvailable { get; private set; }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        SqlServerAvailable = Environment.GetEnvironmentVariable(SkipSqlServerEnvVar) != "1";

        var startups = new List<Task> { StartContainerAsync(_pgContainer, "PostgreSQL") };
        if (SqlServerAvailable)
        {
            startups.Add(StartContainerAsync(_sqlContainer, "SQL Server",
                $"If this host cannot run the amd64-only mssql/server image (e.g. an ARM64 box), "
                + $"set {SkipSqlServerEnvVar}=1 to skip the SQL-Server-backed tests."));
        }
        await Task.WhenAll(startups);

        // Accessing Services triggers the lazy host build.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<SqlServerDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PostgreSqlDbContext>().Database.EnsureCreatedAsync();
    }

    private static async Task StartContainerAsync(IContainer container, string name, string? skipHint = null)
    {
        using var cts = new CancellationTokenSource(ContainerStartTimeout);
        try
        {
            await container.StartAsync(cts.Token);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"The {name} test container failed to start — check the container logs."
                + (skipHint is null ? "" : " " + skipHint),
                e);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSql"] = _pgContainer.GetConnectionString(),
                ["ConnectionStrings:SqlServer"] = SqlServerAvailable ? _sqlContainer.GetConnectionString() : "unused",
            });
        });

        if (!SqlServerAvailable)
        {
            // Drop the original registrations first — the UseSqlServer options service otherwise
            // survives and collides with the stub provider.
            builder.ConfigureServices(services =>
            {
                var stale = services
                    .Where(d => d.ServiceType == typeof(SqlServerDbContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericArguments().Contains(typeof(SqlServerDbContext))))
                    .ToList();
                foreach (var descriptor in stale)
                {
                    services.Remove(descriptor);
                }
                services.AddDbContext<SqlServerDbContext>(options => options
                    .UseInMemoryDatabase("sqlserver-unavailable")
                    .UseStrongTypes());
            });
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _pgContainer.DisposeAsync();
    }
}
