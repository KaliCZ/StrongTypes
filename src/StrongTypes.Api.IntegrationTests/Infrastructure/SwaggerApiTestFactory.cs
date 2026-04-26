using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.OpenApi.TestApi.Swashbuckle;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// In-process host for the Swashbuckle test app. The Swashbuckle assertion
/// suite uses this instead of <see cref="TestWebApplicationFactory"/> so the
/// JSON spec it inspects is unambiguously the Swashbuckle pipeline's output,
/// not Microsoft.AspNetCore.OpenApi's. No databases are needed — the
/// Swashbuckle test app carries no persistence.
/// </summary>
public sealed class SwaggerApiTestFactory : WebApplicationFactory<SwashbuckleTestApiEntryPoint>;
