using StrongTypes.OpenApi.Swashbuckle;

namespace StrongTypes.OpenApi.TestApi.Swashbuckle;

// Distinct entry-point class so this assembly does not emit a `Program` type
// into the global namespace — that would collide with the Microsoft test app's
// `Program` when both projects are referenced by the integration-test assembly.
public class SwashbuckleTestApiEntryPoint
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

        var app = builder.Build();

        app.UseSwagger();

        app.MapControllers();

        app.Run();
    }
}
