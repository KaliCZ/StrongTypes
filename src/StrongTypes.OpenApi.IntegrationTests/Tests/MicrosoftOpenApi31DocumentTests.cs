using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// Runs the shared OpenAPI shape contract against the Microsoft test app
/// configured to emit OpenAPI 3.1. The 3.1 encoding of exclusive numeric
/// bounds (<c>exclusiveMinimum: 0</c> as a number) differs from the 3.0
/// encoding (<c>minimum: 0, exclusiveMinimum: true</c>); both are valid wire
/// shapes and both must round-trip the wrapper contract.
/// </summary>
public sealed class MicrosoftOpenApi31DocumentTests(MicrosoftTestApi31Factory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<MicrosoftTestApi31Factory>
{
    protected override string DocumentUrl => "/openapi/v1.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_1;
    protected override bool IsEmailStringFormatBroken => true;
    protected override bool IsNonBodyParameterStrongTypeSchemaBroken => true;
    protected override bool IsFormPropertyStrongTypeSchemaBroken => true;
}
