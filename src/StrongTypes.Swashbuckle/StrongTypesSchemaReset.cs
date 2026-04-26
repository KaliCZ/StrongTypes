using Microsoft.OpenApi;

namespace StrongTypes.Swashbuckle;

internal static class StrongTypesSchemaReset
{
    // The default schema Swashbuckle produces for a strong-type wrapper is an
    // object with the wrapper's public surface (Value, Length, IsSome, …). None
    // of those appear on the wire, so every filter clears the inherited object
    // shape before painting on the real wire contract.
    public static void ResetToScalar(OpenApiSchema schema)
    {
        schema.Properties?.Clear();
        schema.Required?.Clear();
        schema.AllOf?.Clear();
        schema.OneOf?.Clear();
        schema.AnyOf?.Clear();
        schema.AdditionalProperties = null;
        schema.AdditionalPropertiesAllowed = true;
        schema.Items = null;
        schema.Format = null;
        schema.Minimum = null;
        schema.Maximum = null;
        schema.ExclusiveMinimum = null;
        schema.ExclusiveMaximum = null;
        schema.MinLength = null;
        schema.MaxLength = null;
        schema.MinItems = null;
        schema.MaxItems = null;
        schema.Pattern = null;
        schema.Enum?.Clear();
        schema.Type = null;
    }
}
