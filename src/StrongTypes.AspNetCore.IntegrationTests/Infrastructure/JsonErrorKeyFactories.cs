using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace StrongTypes.AspNetCore.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the test API with JSON error-key normalization on — the out-of-the-box
/// default of <see cref="StrongTypesServiceCollectionExtensions.AddStrongTypes(IServiceCollection, System.Action{StrongTypesAspNetCoreOptions})"/>.
/// </summary>
public sealed class NormalizedJsonErrorKeysFactory : WebApplicationFactory<Program>;

/// <summary>Boots the test API with normalization explicitly turned off, exposing the raw System.Text.Json paths.</summary>
public sealed class RawJsonErrorKeysFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.AddSingleton(new StrongTypesAspNetCoreOptions { NormalizeJsonErrorKeys = false }));
}
