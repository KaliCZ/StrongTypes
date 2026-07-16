using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public sealed class SwashbuckleOpenApiDocumentTests(SwashbuckleTestApiFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<SwashbuckleTestApiFactory>
{
    protected override string DocumentUrl => "/swagger/v1/swagger.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_0;
    protected override bool IsPlainIntFormSchemaMissingType => false;
}
