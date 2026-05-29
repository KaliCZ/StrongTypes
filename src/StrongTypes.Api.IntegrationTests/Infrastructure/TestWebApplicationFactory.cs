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
/// local host that cannot run the amd64-only <c>mssql/server</c> image (e.g. an
/// ARM64 dev box), where it may be skipped via an explicit opt-in. See
/// <see cref="SqlServerAvailable"/> for what callers must guard, and the
/// <c>STRONGTYPES_SKIP_SQLSERVER_IF_UNAVAILABLE</c> env var below for the gate.
/// CI never takes the skip path: there, a SQL Server that fails to start is a
/// hard failure of the whole test run, never a silent skip.
/// </remarks>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DockerGroupLabel = "com.docker.compose.project";
    private const string DockerGroupName = "StrongTypes";

    // Local, non-CI opt-in to skip SQL Server; the gate is SqlServerSkipPermitted.
    private const string SkipSqlServerEnvVar = "STRONGTYPES_SKIP_SQLSERVER_IF_UNAVAILABLE";

    private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromSeconds(45);

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    /// <summary>
    /// Whether the SQL Server container is up and backed by a real SQL Server.
    /// <see langword="false"/> only on a local host that opted into skipping and
    /// where SQL Server could not start — in CI this is always <see langword="true"/>
    /// or the fixture throws. Tests must skip every SQL-Server-specific assertion
    /// when this is <see langword="false"/>; the in-memory stub that keeps the
    /// dual-write API booting does not exercise the real SQL Server wire path.
    /// </summary>
    public bool SqlServerAvailable { get; private set; }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // PostgreSQL is mandatory on every host; a start failure always throws.
        await StartContainerAsync(_pgContainer, "PostgreSQL");

        try
        {
            await StartContainerAsync(_sqlContainer, "SQL Server");
            SqlServerAvailable = true;
        }
        catch (Exception ex)
        {
            if (!SqlServerSkipPermitted)
            {
                throw new InvalidOperationException(
                    "The SQL Server test container failed to start and skipping is not permitted in this environment. " +
                    $"Set {SkipSqlServerEnvVar}=1 to allow skipping the SQL-Server-backed tests on a local, non-CI host " +
                    "(e.g. an ARM64 box that cannot run the amd64-only mssql/server image). CI always requires SQL Server.",
                    ex);
            }
            SqlServerAvailable = false;
        }

        // Accessing Services triggers the lazy host build.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<SqlServerDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PostgreSqlDbContext>().Database.EnsureCreatedAsync();
    }

    // Opt-in set AND not CI. Default and any CI env both forbid skipping, so a
    // missing or stray flag fails safe toward a hard crash rather than a skip.
    private static bool SqlServerSkipPermitted => SkipOptIn && !RunningInCi;

    private static bool SkipOptIn => EnvFlag(SkipSqlServerEnvVar);

    private static bool RunningInCi =>
        EnvFlag("CI")
        || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is { Length: > 0 }
        || Environment.GetEnvironmentVariable("TF_BUILD") is { Length: > 0 };

    private static bool EnvFlag(string name) =>
        Environment.GetEnvironmentVariable(name) is { } value
        && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));

    private static async Task StartContainerAsync(IContainer container, string name)
    {
        using var cts = new CancellationTokenSource(ContainerStartTimeout);
        try
        {
            await container.StartAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The {name} test container did not start within {ContainerStartTimeout.TotalSeconds:0}s. " +
                "It either failed to start or never began accepting connections — check the container logs. " +
                "On ARM64 hosts this can also happen when the image has no native ARM build, as the emulated process may crash on startup.");
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
