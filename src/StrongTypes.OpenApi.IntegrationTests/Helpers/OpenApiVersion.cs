namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// The OpenAPI specification version a pipeline emits. Controls
/// version-strict assertions across the helper layer — exclusive
/// numeric bounds, null-branch markers, and the version-marker
/// contamination check on nullable union wrappers.
/// </summary>
public enum OpenApiVersion { V3_0, V3_1 }
