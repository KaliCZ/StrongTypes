using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// Runs the shared OpenAPI shape contract against the Microsoft test app,
/// wired to <c>Microsoft.AspNetCore.OpenApi</c> (<c>AddOpenApi()</c>) +
/// <c>Kalicz.StrongTypes.OpenApi.Microsoft</c>'s schema transformers.
/// </summary>
public sealed class MicrosoftOpenApiDocumentTests(MicrosoftTestApiFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<MicrosoftTestApiFactory>
{
    protected override string DocumentUrl => "/openapi/v1.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_0;
    protected override bool IsPlainStringEmailFormatBroken => true;
}
