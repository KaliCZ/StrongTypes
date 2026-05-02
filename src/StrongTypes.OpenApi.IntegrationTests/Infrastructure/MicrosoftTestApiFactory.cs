using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.OpenApi.TestApi.Microsoft;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

/// <summary>
/// In-process host for the Microsoft test app. Used by the Microsoft side of
/// the shared OpenAPI assertion suite so the JSON spec it inspects is
/// unambiguously the Microsoft.AspNetCore.OpenApi pipeline's output.
/// </summary>
public sealed class MicrosoftTestApiFactory : WebApplicationFactory<MicrosoftTestApiEntryPoint>;
