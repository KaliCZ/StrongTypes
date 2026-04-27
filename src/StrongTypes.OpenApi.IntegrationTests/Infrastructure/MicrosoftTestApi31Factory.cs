using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.TestApi.Microsoft;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

/// <summary>
/// Microsoft test app forced to emit OpenAPI 3.1 instead of the entry-point's
/// default of 3.0. Re-runs the same shared shape contract so any divergence
/// between the two encodings (numeric <c>exclusiveMinimum</c> in 3.1 vs the
/// <c>minimum</c>+<c>exclusiveMinimum:true</c> pair in 3.0) shows up as a
/// per-version test failure.
/// </summary>
public sealed class MicrosoftTestApi31Factory : WebApplicationFactory<MicrosoftTestApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.PostConfigureAll<OpenApiOptions>(options =>
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1));
    }
}
