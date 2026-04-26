using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests.OpenApi;

/// <summary>
/// Runs the shared OpenAPI shape contract against StrongTypes.Api, which is
/// wired to <c>Microsoft.AspNetCore.OpenApi</c> (<c>AddOpenApi()</c>) +
/// <c>Kalicz.StrongTypes.OpenApi</c>'s schema transformers.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MicrosoftOpenApiDocumentTests(TestWebApplicationFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient())
{
    protected override string DocumentUrl => "/openapi/v1.json";
}
