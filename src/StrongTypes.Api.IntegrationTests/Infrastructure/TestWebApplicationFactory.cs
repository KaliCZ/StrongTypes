using DotNet.Testcontainers.Containers;
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
    private const string DockerGroupLabel = "com.docker.compose.project";
    private const string DockerGroupName = "StrongTypes";

    private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromSeconds(45);

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithLabel(DockerGroupLabel, DockerGroupName)
        .Build();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Start both containers in parallel; containers must be up before the
        // host is built so that ConfigureAppConfiguration can read their connection strings.
        await Task.WhenAll(
            StartContainerAsync(_sqlContainer, "SQL Server"),
            StartContainerAsync(_pgContainer, "PostgreSQL"));

        // Accessing Services triggers the lazy host build.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<SqlServerDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PostgreSqlDbContext>().Database.EnsureCreatedAsync();
    }

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
