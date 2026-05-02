using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.OpenApi.TestApi.Swashbuckle;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

/// <summary>
/// In-process host for the Swashbuckle test app. Used by the Swashbuckle side
/// of the shared OpenAPI assertion suite so the JSON spec it inspects is
/// unambiguously the Swashbuckle pipeline's output.
/// </summary>
public sealed class SwashbuckleTestApiFactory : WebApplicationFactory<SwashbuckleTestApiEntryPoint>;
