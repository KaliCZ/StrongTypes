using StrongTypes.OpenApi.Microsoft;
using StrongTypes.OpenApi.TestApi.Shared;

namespace StrongTypes.OpenApi.TestApi.Microsoft;

// Distinct entry-point class so this assembly does not emit a `Program` type
// into the global namespace — that would collide with the Swashbuckle test
// app's `Program` when both projects are referenced by the integration-test
// assembly.
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
