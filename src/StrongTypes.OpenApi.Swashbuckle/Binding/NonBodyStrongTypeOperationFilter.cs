using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Layers caller-supplied data-annotations
/// (<c>[StringLength]</c>, <c>[Range]</c>, <c>[RegularExpression]</c>, …)
/// onto strong-type wrappers that arrive at non-body slots —
/// <c>[FromQuery]</c> / <c>[FromRoute]</c> / <c>[FromHeader]</c>
/// parameters and the per-field schemas of <c>[FromForm]</c> request
/// bodies. The wrapper's wire shape itself is painted by the per-type
/// schema filters (<see cref="NonEmptyStringSchemaFilter"/> et al);
/// without this pass, caller bounds attached at the slot would be
/// dropped — Swashbuckle's <see cref="PropertyAnnotationSchemaFilter"/>
/// only sees the parent type's <c>properties</c> map and never reaches
/// the parameter slots or the form-body's <c>allOf</c> entries.
///
/// Body schemas are out of scope by construction: this filter only
/// dispatches on <see cref="BindingSource.Query"/> /
/// <see cref="BindingSource.Path"/> / <see cref="BindingSource.Header"/> /
/// <see cref="BindingSource.Form"/>, and the form path is gated to
/// <c>multipart/form-data</c> / <c>application/x-www-form-urlencoded</c>.
///
/// The form path also reshapes Swashbuckle's <c>{ allOf: [&lt;each-field&gt;] }</c>
/// request body — emitted whenever every form field is component-typed —
/// back into a proper <c>{ type: object, properties: { … } }</c> map keyed
/// by the form-field names from the <see cref="ApiParameterDescription"/>s.
/// Without that, consumers see a nameless allOf and can't tell which schema
/// belongs to which field.
/// </summary>
public sealed class NonBodyStrongTypeOperationFilter : IOperationFilter
{
    private static readonly string[] s_formContentTypes =
    [
        "multipart/form-data",
        "application/x-www-form-urlencoded",
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptions = context.ApiDescription.ParameterDescriptions;
        if (descriptions is null || descriptions.Count == 0) return;

        ReshapeFormAllOfIntoProperties(operation, descriptions, context.SchemaGenerator, context.SchemaRepository);

        foreach (var pd in descriptions)
        {
            if (pd.Source is null) continue;

            if (pd.Source == BindingSource.Query
                || pd.Source == BindingSource.Path
                || pd.Source == BindingSource.Header)
            {
                MergeOperationParameterAnnotations(operation, pd);
            }
            else if (pd.Source == BindingSource.Form)
            {
                MergeFormPropertyAnnotations(operation, pd);
            }
        }
    }

    private static void MergeOperationParameterAnnotations(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.Parameters is null) return;
        var clrType = ResolveParameterClrType(pd);
        if (clrType is null) return;
        if (GetSlotAttributes(pd).Count == 0) return;

        foreach (var p in operation.Parameters)
        {
            if (p is not OpenApiParameter parameter) continue;
            if (!string.Equals(parameter.Name, pd.Name, StringComparison.Ordinal)) continue;

            parameter.Schema = ApplyAnnotations(parameter.Schema, clrType, pd);
            return;
        }
    }

    private static void MergeFormPropertyAnnotations(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.RequestBody?.Content is not { } content) return;
        var clrType = ResolveParameterClrType(pd);
        if (clrType is null) return;
        if (GetSlotAttributes(pd).Count == 0) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.Properties is not { Count: > 0 } properties) continue;
            if (!properties.TryGetValue(pd.Name, out var propSchema)) continue;

            properties[pd.Name] = ApplyAnnotations(propSchema, clrType, pd);
            return;
        }
    }

    /// <summary>
    /// Replaces Swashbuckle's all-component <c>{ allOf: [&lt;each-field&gt;] }</c>
    /// form-body schema (and the hybrid <c>{ allOf: [wrappers, {primitives}] }</c>
    /// shape Swashbuckle emits for mixed forms) with a flat
    /// <c>{ type: object, properties: { … } }</c> map keyed by each form
    /// field's <see cref="ApiParameterDescription.Name"/>. Per-property
    /// schemas come from the schema generator: wrappers stay as
    /// <c>$ref</c>s to their components (or inline schemas for
    /// collection-shaped wrappers Swashbuckle inlines anyway), and
    /// primitives are generated with their <see cref="MemberInfo"/> so
    /// Swashbuckle's own data-annotation pipeline applies
    /// <c>[StringLength]</c>, <c>[Range]</c>, etc. directly to the
    /// emitted schema.
    /// </summary>
    private static void ReshapeFormAllOfIntoProperties(
        OpenApiOperation operation,
        IList<ApiParameterDescription> descriptions,
        ISchemaGenerator schemaGenerator,
        SchemaRepository schemaRepository)
    {
        if (operation.RequestBody?.Content is not { } content) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.AllOf is not { Count: > 0 }) continue;
            if (formSchema.Properties is { Count: > 0 }) continue;

            var properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
            foreach (var pd in descriptions)
            {
                if (pd.Source != BindingSource.Form) continue;
                var clrType = ResolveParameterClrType(pd);
                if (clrType is null) continue;

                // For primitives, hand Swashbuckle the form record's
                // PropertyInfo so its generator surfaces caller annotations
                // (`[StringLength]`, `[Range]`, …) directly. For wrappers
                // we want a clean $ref — `MergeFormPropertyAnnotations`
                // layers slot annotations on top via WrapperAnnotationApplier,
                // and passing MemberInfo here would risk double-emission
                // without the inline marker the inliner needs.
                MemberInfo? memberInfo = null;
                if (!StrongTypeSchemaTypes.IsInlineable(clrType))
                    memberInfo = ResolveFormPropertyMember(pd);

                properties[pd.Name] = schemaGenerator.GenerateSchema(clrType, schemaRepository, memberInfo);
            }

            if (properties.Count == 0) continue;

            formSchema.Properties = properties;
            formSchema.AllOf = null;
            formSchema.Type ??= JsonSchemaType.Object;
        }
    }

    private static MemberInfo? ResolveFormPropertyMember(ApiParameterDescription pd)
    {
        if (pd.ModelMetadata is not { ContainerType: { } containerType, PropertyName: { } propertyName }) return null;
        return containerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    }

    private static IOpenApiSchema ApplyAnnotations(IOpenApiSchema? slot, Type clrType, ApiParameterDescription pd)
    {
        var attributes = GetSlotAttributes(pd);
        if (attributes.Count == 0) return slot ?? new OpenApiSchema();

        // Wrapper-typed slots arrive at this filter as `OpenApiSchemaReference`
        // ($ref to the wrapper component painted by the per-type schema
        // filter). Mirror PropertyAnnotationSchemaFilter's body-side trick:
        // wrap the ref in `allOf:[<ref>]+annotations` and mark with the
        // inline marker so StrongTypeInliner collapses it back to a flat
        // shape later. Inline OpenApiSchema slots can be mutated directly.
        if (slot is OpenApiSchema concrete)
        {
            WrapperAnnotationApplier.TryApply(concrete, clrType, attributes);
            return concrete;
        }

        if (slot is OpenApiSchemaReference)
        {
            var wrapper = new OpenApiSchema { AllOf = [slot] };
            if (WrapperAnnotationApplier.TryApply(wrapper, clrType, attributes))
            {
                StrongTypeInlineMarker.Set(wrapper);
                return wrapper;
            }
            return slot;
        }

        return slot ?? new OpenApiSchema();
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

    private static Type? ResolveParameterClrType(ApiParameterDescription pd)
        => pd.ModelMetadata?.ModelType ?? pd.ParameterDescriptor?.ParameterType ?? pd.Type;
}
