using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Rewrites the schema for <see cref="Maybe{T}"/> to the on-the-wire
/// wrapper object the JSON converter emits:
/// <code>
/// { "type": "object", "properties": { "Value": &lt;T schema&gt; } }
/// </code>
/// The <c>Value</c> property is not required — omitting it is how the
/// converter encodes <c>None</c>.
/// </summary>
public sealed class MaybeSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // Maybe<T> is a struct that also implements IEnumerable<T>, so a
        // Maybe<T>? property arrives here as Nullable<Maybe<T>> with the
        // schema already coerced into the array shape Swashbuckle gives any
        // IEnumerable. Unwrap Nullable<> first so the filter still recognises
        // the underlying Maybe<T> and rewrites the schema to its real wire form.
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Maybe<>))
            return;
        if (schema is not OpenApiSchema concrete) return;

        var innerType = type.GetGenericArguments()[0];
        var innerSchema = context.SchemaGenerator.GenerateSchema(
            innerType, context.SchemaRepository, memberInfo: null, parameterInfo: null, routeInfo: null);

        SchemaPaint.ClearWrapperShape(concrete);
        concrete.Type = JsonSchemaType.Object;
        concrete.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["Value"] = innerSchema,
        };
        StrongTypeInlineMarker.Set(concrete);
    }
}
