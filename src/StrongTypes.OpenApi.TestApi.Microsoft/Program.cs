using StrongTypes.OpenApi.Microsoft;
using StrongTypes.OpenApi.TestApi.Shared;

namespace StrongTypes.OpenApi.TestApi.Microsoft;

// Named entry point (not top-level statements): a global `Program` would collide with the
// Swashbuckle test app's when the integration-test assembly references both.
public class MicrosoftTestApiEntryPoint
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(NonEmptyStringEntityController).Assembly);
        builder.Services.AddOpenApi(options =>
        {
            options.OpenApiVersion = global::Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            options.AddStrongTypes();
        });

        var app = builder.Build();

        app.MapControllers();
        app.MapOpenApi();

        app.Run();
    }
}
