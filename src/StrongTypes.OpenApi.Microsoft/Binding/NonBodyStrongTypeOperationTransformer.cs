using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Repaints schemas attached to non-body parameters (<c>[FromQuery]</c>,
/// <c>[FromRoute]</c>, <c>[FromHeader]</c>) and to the per-field schemas
/// of <c>[FromForm]</c> request bodies. Microsoft.AspNetCore.OpenApi only
/// fires <see cref="IOpenApiSchemaTransformer"/> on JSON-body schemas; for
/// every other slot the parameter's CLR type is inspected via the
/// <see cref="ApiParameterDescription"/> exposed on the operation context
/// and the wire schema is written directly.
/// </summary>
internal sealed class NonBodyStrongTypeOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly string[] s_formContentTypes =
    [
        "multipart/form-data",
        "application/x-www-form-urlencoded",
    ];

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var descriptions = context.Description.ParameterDescriptions;
        if (descriptions is null || descriptions.Count == 0) return Task.CompletedTask;

        foreach (var pd in descriptions)
        {
            if (pd.Source is null) continue;

            if (pd.Source == BindingSource.Query
                || pd.Source == BindingSource.Path
                || pd.Source == BindingSource.Header)
            {
                RewriteOperationParameter(operation, pd);
            }
            else if (pd.Source == BindingSource.Form)
            {
                RewriteFormProperty(operation, pd);
            }
        }

        return Task.CompletedTask;
    }

    private static void RewriteOperationParameter(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.Parameters is null) return;

        var painted = TryBuildWireSchema(ResolveParameterClrType(pd));
        if (painted is null) return;

        foreach (var p in operation.Parameters)
        {
            if (p is not OpenApiParameter parameter) continue;
            if (!string.Equals(parameter.Name, pd.Name, StringComparison.Ordinal)) continue;

            parameter.Schema = painted;
            return;
        }
    }

    private static void RewriteFormProperty(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.RequestBody?.Content is not { } content) return;

        var painted = TryBuildWireSchema(ResolveParameterClrType(pd));
        if (painted is null) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.Properties is not { Count: > 0 } properties) continue;

            var key = ResolveFormPropertyKey(pd.Name, properties);
            if (key is null) continue;

            properties[key] = painted;
        }
    }

    private static string? ResolveFormPropertyKey(string name, IDictionary<string, IOpenApiSchema> properties)
    {
        if (properties.ContainsKey(name)) return name;

        var camel = JsonNamingPolicy.CamelCase.ConvertName(name);
        if (properties.ContainsKey(camel)) return camel;

        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            var leaf = name[(lastDot + 1)..];
            if (properties.ContainsKey(leaf)) return leaf;
            var leafCamel = JsonNamingPolicy.CamelCase.ConvertName(leaf);
            if (properties.ContainsKey(leafCamel)) return leafCamel;
        }

        foreach (var key in properties.Keys)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) return key;
        }

        return null;
    }

    // ApiParameterDescription.Type is the type the model binder reports —
    // for strong-types that implement IParsable<T>, ASP.NET reports the
    // string overload's input, hiding the wrapper. ModelMetadata.ModelType
    // exposes the actual CLR type for both parameter slots and for
    // properties of a flattened [FromForm] model.
    private static Type? ResolveParameterClrType(ApiParameterDescription pd)
        => pd.ModelMetadata?.ModelType ?? pd.ParameterDescriptor?.ParameterType ?? pd.Type;

    private static OpenApiSchema? TryBuildWireSchema(Type? clrType)
    {
        if (clrType is null) return null;

        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (unwrapped == typeof(NonEmptyString))
            return new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 };

        if (unwrapped == typeof(Email))
            // Format is intentionally omitted to stay consistent with the
            // JSON-body Email schema this pipeline emits — Microsoft.AspNetCore.OpenApi
            // doesn't honor format:email there either (see EmailSchemaTransformer +
            // the IsEmailStringFormatBroken flag in the integration tests).
            return new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = 1,
                MaxLength = Email.MaxLength,
            };

        if (unwrapped.IsGenericType && NumericWrapperKinds.TryGetBound(unwrapped.GetGenericTypeDefinition(), out var bound))
        {
            var schema = new OpenApiSchema();
            NumericWrapperPainter.Paint(schema, unwrapped.GetGenericArguments()[0], bound);
            return schema;
        }

        return null;
    }
}
