using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// Runs the shared OpenAPI shape contract against the Swashbuckle test app,
/// wired to <c>Swashbuckle.AspNetCore</c> (<c>AddSwaggerGen()</c>) +
/// <c>Kalicz.StrongTypes.OpenApi.Swashbuckle</c>'s schema filters.
/// </summary>
public sealed class SwashbuckleOpenApiDocumentTests(SwashbuckleTestApiFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<SwashbuckleTestApiFactory>
{
    protected override string DocumentUrl => "/swagger/v1/swagger.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_0;
    protected override bool IsFormPropertiesSchemaBroken => true;
    protected override bool IsNonBodyAnnotationMergingBroken => true;
}
