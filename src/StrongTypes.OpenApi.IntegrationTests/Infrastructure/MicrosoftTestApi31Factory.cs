using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.TestApi.Microsoft;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

public sealed class MicrosoftTestApi31Factory : WebApplicationFactory<MicrosoftTestApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.PostConfigureAll<OpenApiOptions>(options =>
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1));
    }
}
