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
/// Shared fixture that boots the database containers once per test collection,
/// overrides the connection strings via configuration (so Program.cs's DbContext
/// registrations pick them up unchanged), and creates the schema via EnsureCreated.
/// </summary>
/// <remarks>
/// PostgreSQL is required everywhere. SQL Server is required too — except on a
/// host that cannot run the amd64-only <c>mssql/server</c> image (e.g. an ARM64
/// dev box), where it can be skipped via an explicit opt-in. See
/// <see cref="SqlServerAvailable"/> for what callers must guard, and the
/// <c>STRONGTYPES_SKIP_SQLSERVER</c> env var below for the gate. Absent the
/// opt-in the container is started, and any failure to start is a hard failure
/// of the whole test run, never a silent skip.
/// </remarks>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DockerGroupLabel = "com.docker.compose.project";
    private const string DockerGroupName = "StrongTypes";

    // Opt-in to skip SQL Server entirely (the container is never started).
    private const string SkipSqlServerEnvVar = "STRONGTYPES_SKIP_SQLSERVER";

    private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromSeconds(45);

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    /// <summary>
    /// Whether the SQL Server container is up and backed by a real SQL Server.
    /// <see langword="false"/> only on a host that opted into skipping via the
    /// env var, where the container is never started. Tests must skip every
    /// SQL-Server-specific assertion when this is <see langword="false"/>; the
    /// in-memory stub that keeps the dual-write API booting does not exercise
    /// the real SQL Server wire path.
    /// </summary>
    public bool SqlServerAvailable { get; private set; }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Skipping SQL Server is opt-in; absent the flag the container is started
        // and any failure to start is a hard crash (see StartContainerAsync).
        SqlServerAvailable = Environment.GetEnvironmentVariable(SkipSqlServerEnvVar) != "1";

        // PostgreSQL is mandatory on every host; SQL Server too unless skipped.
        // Start them concurrently — a start failure or timeout on either throws.
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
            // Swap SQL Server for an in-memory stub so the dual-write endpoints
            // still boot. Drop every registration keyed by SqlServerDbContext
            // first — otherwise the original UseSqlServer call survives in its
            // options-configuration service and collides with the stub provider.
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
