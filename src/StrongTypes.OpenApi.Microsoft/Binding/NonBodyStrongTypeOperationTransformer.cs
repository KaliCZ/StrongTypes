using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
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
/// and the wire shape is written directly. Caller annotations attached at
/// the slot (e.g. <c>[StringLength]</c> on a <c>[FromQuery]</c> parameter
/// or <c>[Range]</c> on a <c>[FromForm]</c> property) are layered on top
/// via <see cref="WrapperAnnotationApplier"/> so non-body slots merge
/// annotations the same way JSON-body properties do.
///
/// Body schemas are out of scope by construction: this transformer only
/// dispatches on <see cref="BindingSource.Query"/> / <see cref="BindingSource.Path"/> /
/// <see cref="BindingSource.Header"/> / <see cref="BindingSource.Form"/>,
/// and the form path is gated to <c>multipart/form-data</c> /
/// <c>application/x-www-form-urlencoded</c>. JSON request/response bodies
/// are reached neither by source nor by content type.
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
        if (clrType is null) return;

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
        if (clrType is null) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.Properties is not { Count: > 0 } properties) continue;

            // Microsoft.AspNetCore.OpenApi keys form-body properties by the
            // C# property name (PascalCase), matching ApiParameterDescription.Name.
            // No camelCase / case-insensitive fallback because this pipeline
            // doesn't produce those — and a fallback would risk binding to
            // the wrong slot if a user DTO ever has near-collisions.
            if (!properties.ContainsKey(pd.Name)) continue;
            properties[pd.Name] = PaintSlot(properties[pd.Name], clrType, pd);
        }
    }

    private static IOpenApiSchema PaintSlot(IOpenApiSchema? existing, Type clrType, ApiParameterDescription pd)
    {
        // Maybe<T> bound from a non-body slot via the StrongTypes.AspNetCore
        // model binder reads a single raw form-data value and wraps it as
        // Some/None — the wire is the inner T, not the body-side
        // {"Value":<T>} wrapper object the JSON converter emits. Unwrap
        // here so the rest of this pass paints the inner shape and merges
        // slot annotations against it.
        if (StrongTypeSchemaTypes.TryGetMaybeValue(clrType, out var maybeInner))
            clrType = maybeInner;

        // Mutate the existing schema when possible so any keywords the
        // pipeline already wrote (description, default, caller-applied
        // [StringLength] on the IParsable string overload, …) survive the
        // wrapper paint. Falls back to a fresh schema only when the slot
        // currently holds an OpenApiSchemaReference or is null.
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
        // For a flattened [FromForm] complex model, ModelMetadata pinpoints
        // the property; reflect on it to read the property's own attributes.
        if (pd.ModelMetadata is { ContainerType: { } containerType, PropertyName: { } propertyName })
        {
            var prop = containerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is not null)
                return prop.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
        }

        // For a method parameter (query/path/header), the attributes live on
        // the ParameterInfo.
        if (pd.ParameterDescriptor is ControllerParameterDescriptor cpd)
            return cpd.ParameterInfo.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();

        return [];
    }

    // ApiParameterDescription.Type is the type the model binder reports —
    // for strong-types that implement IParsable<T>, ASP.NET reports the
    // string overload's input, hiding the wrapper. ModelMetadata.ModelType
    // exposes the actual CLR type for both parameter slots and for
    // properties of a flattened [FromForm] model.
    private static Type? ResolveParameterClrType(ApiParameterDescription pd)
        => pd.ModelMetadata?.ModelType ?? pd.ParameterDescriptor?.ParameterType ?? pd.Type;
}
