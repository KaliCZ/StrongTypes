using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests.OpenApi;

/// <summary>
/// Runs the shared OpenAPI shape contract against StrongTypes.SwaggerApi, which
/// is wired to <c>Swashbuckle.AspNetCore</c> (<c>AddSwaggerGen()</c>) +
/// <c>Kalicz.StrongTypes.Swashbuckle</c>'s schema filters.
/// </summary>
public sealed class SwashbuckleOpenApiDocumentTests(SwaggerApiTestFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<SwaggerApiTestFactory>
{
    protected override string DocumentUrl => "/swagger/v1/swagger.json";
}
