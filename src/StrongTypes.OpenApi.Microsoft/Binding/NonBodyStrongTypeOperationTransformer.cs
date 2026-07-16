using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Repaints schemas attached to non-body slots — <c>[FromQuery]</c>/<c>[FromRoute]</c>/<c>[FromHeader]</c>
/// parameters and <c>[FromForm]</c> fields — because Microsoft.AspNetCore.OpenApi only fires
/// <see cref="IOpenApiSchemaTransformer"/> on JSON-body schemas. Caller annotations attached
/// at the slot are layered on top, matching the JSON-body behavior.
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
        var clrType = ResolveParameterClrType(pd);

        foreach (var p in operation.Parameters)
        {
            if (p is not OpenApiParameter parameter) continue;
            if (!string.Equals(parameter.Name, pd.Name, StringComparison.Ordinal)) continue;

            parameter.Schema = PaintSlot(parameter.Schema, clrType, pd);
            return;
        }
    }

    private static void RewriteFormProperty(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.RequestBody?.Content is not { } content) return;
        var clrType = ResolveParameterClrType(pd);

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.Properties is not { Count: > 0 } properties) continue;

            // Form-body properties are keyed by the C# property name (PascalCase), matching ApiParameterDescription.Name.
            if (!properties.ContainsKey(pd.Name)) continue;
            properties[pd.Name] = PaintSlot(properties[pd.Name], clrType, pd);
        }
    }

    private static IOpenApiSchema PaintSlot(IOpenApiSchema? existing, Type clrType, ApiParameterDescription pd)
    {
        // Mutate the existing schema so keywords the pipeline already wrote survive the wrapper paint.
        var schema = existing as OpenApiSchema ?? new OpenApiSchema();
        if (!TryPaintWireShape(schema, clrType)) return existing ?? schema;

        var attributes = GetSlotAttributes(pd);
        if (attributes.Count > 0)
            WrapperAnnotationApplier.TryApply(schema, clrType, attributes);

        return schema;
    }

    private static bool TryPaintWireShape(OpenApiSchema schema, Type clrType)
    {
        if (StrongTypeSchemaTypes.IsNonEmptyString(clrType))
        {
            SchemaPaint.ClearWrapperShape(schema);
            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            SchemaPaint.TightenMinLength(schema, 1);
            return true;
        }

        if (StrongTypeSchemaTypes.IsEmail(clrType))
        {
            SchemaPaint.ClearWrapperShape(schema);
            schema.Type = JsonSchemaType.String;
            SchemaPaint.SetFormatIfAbsent(schema, "email");
            SchemaPaint.TightenMinLength(schema, 1);
            SchemaPaint.TightenMaxLength(schema, Email.MaxLength);
            return true;
        }

        if (StrongTypeSchemaTypes.IsDigit(clrType))
        {
            SchemaPaint.ClearWrapperShape(schema);
            schema.Type = JsonSchemaType.Integer;
            schema.Format = "int32";
            SchemaPaint.TightenLowerBound(schema, 0, floorExclusive: false);
            SchemaPaint.TightenUpperBound(schema, 9, floorExclusive: false);
            return true;
        }

        if (StrongTypeSchemaTypes.TryGetNumeric(clrType, out var valueType, out var bound))
        {
            NumericWrapperPainter.Paint(schema, valueType, bound);
            return true;
        }

        if (StrongTypeSchemaTypes.TryGetNonEmptyEnumerableElement(clrType, out var elementType))
        {
            SchemaPaint.ClearWrapperShape(schema);
            schema.Type = JsonSchemaType.Array;
            SchemaPaint.TightenMinItems(schema, 1);
            var itemsSchema = new OpenApiSchema();
            if (TryPaintWireShape(itemsSchema, elementType))
                schema.Items = itemsSchema;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<Attribute> GetSlotAttributes(ApiParameterDescription pd)
    {
        if (pd.ModelMetadata is { ContainerType: { } containerType, PropertyName: { } propertyName })
        {
            var prop = containerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is not null)
                return prop.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
        }

        if (pd.ParameterDescriptor is ControllerParameterDescriptor cpd)
            return cpd.ParameterInfo.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();

        return [];
    }

    // Not pd.Type: it lies for IParsable<T> strong types, reporting the string overload's input instead of the wrapper.
    private static Type ResolveParameterClrType(ApiParameterDescription pd)
        => pd.ModelMetadata.ModelType;
}
