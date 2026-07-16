using StrongTypes.OpenApi.Swashbuckle;
using StrongTypes.OpenApi.TestApi.Shared;

namespace StrongTypes.OpenApi.TestApi.Swashbuckle;

// Named entry point (not top-level statements): a global `Program` would collide with the
// Microsoft test app's when the integration-test assembly references both.
public class SwashbuckleTestApiEntryPoint
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(NonEmptyStringEntityController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => options.AddStrongTypes());

        var app = builder.Build();

        app.UseSwagger();

        app.MapControllers();

        app.Run();
    }
}
