using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
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
/// </summary>
public sealed class NonBodyStrongTypeOperationFilter(ILogger<NonBodyStrongTypeOperationFilter>? logger = null) : IOperationFilter
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

        // For form bodies that emit the broken `{allOf:[<each-component-field>]}`
        // shape (every form field is component-typed — see
        // IsFormPropertiesSchemaBroken in the integration tests), we need to
        // map allOf entries back to their CLR property by declaration order.
        // Compute that map once per operation.
        var formAllOfIndexByProperty = BuildFormAllOfIndexMap(descriptions);

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
                MergeFormPropertyAnnotations(operation, pd, formAllOfIndexByProperty);
            }
        }
    }

    private static void MergeOperationParameterAnnotations(OpenApiOperation operation, ApiParameterDescription pd)
    {
        if (operation.Parameters is null) return;
        var clrType = ResolveParameterClrType(pd);
        if (clrType is null) return;

        foreach (var p in operation.Parameters)
        {
            if (p is not OpenApiParameter parameter) continue;
            if (!string.Equals(parameter.Name, pd.Name, StringComparison.Ordinal)) continue;

            parameter.Schema = ApplyAnnotations(parameter.Schema, clrType, pd);
            return;
        }
    }

    private void MergeFormPropertyAnnotations(OpenApiOperation operation, ApiParameterDescription pd, IReadOnlyDictionary<string, int>? allOfIndexByProperty)
    {
        if (operation.RequestBody?.Content is not { } content) return;
        var clrType = ResolveParameterClrType(pd);
        if (clrType is null) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;

            // Modern Swashbuckle path: a proper `properties` map keyed by
            // the form model property name.
            if (formSchema.Properties is { Count: > 0 } properties)
            {
                if (properties.TryGetValue(pd.Name, out var propSchema))
                {
                    properties[pd.Name] = ApplyAnnotations(propSchema, clrType, pd);
                    return;
                }
            }

            // Broken path: form body emits `{allOf:[<each-component-field>]}`
            // when every field is component-typed. Look the field up by the
            // index we built from the API description's declaration order.
            if (formSchema.AllOf is { Count: > 0 } allOf
                && allOfIndexByProperty is not null
                && allOfIndexByProperty.TryGetValue(pd.Name, out var index))
            {
                if (!IsExpectedBrokenFormAllOfTarget(allOf, allOfIndexByProperty.Count, pd.Name, index))
                    return;

                allOf[index] = ApplyAnnotations(allOf[index], clrType, pd);
            }
        }
    }

    private bool IsExpectedBrokenFormAllOfTarget(IList<IOpenApiSchema> allOf, int expectedCount, string propertyName, int index)
    {
        if (allOf.Count != expectedCount)
        {
            logger?.LogError("StrongTypes form annotation merge skipped for property '{PropertyName}' because the form allOf count ({ActualCount}) did not match the strong-type form field count ({ExpectedCount}).",
                propertyName, allOf.Count, expectedCount);
            return false;
        }

        if (index < 0 || index >= allOf.Count)
        {
            logger?.LogError("StrongTypes form annotation merge skipped for property '{PropertyName}' because the computed allOf index ({Index}) was outside the form allOf count ({ActualCount}).",
                propertyName, index, allOf.Count);
            return false;
        }

        if (allOf[index] is not OpenApiSchemaReference)
        {
            logger?.LogError("StrongTypes form annotation merge skipped for property '{PropertyName}' because the target form allOf entry at index {Index} was not a reference schema.",
                propertyName, index);
            return false;
        }

        return true;
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

    private static IReadOnlyDictionary<string, int>? BuildFormAllOfIndexMap(IList<ApiParameterDescription> descriptions)
    {
        Dictionary<string, int>? map = null;
        var index = 0;
        foreach (var pd in descriptions)
        {
            if (pd.Source != BindingSource.Form) continue;
            if (!IsWrapperType(ResolveParameterClrType(pd))) continue;
            map ??= new Dictionary<string, int>(StringComparer.Ordinal);
            map[pd.Name] = index++;
        }
        return map;
    }

    private static bool IsWrapperType(Type? clrType)
    {
        if (clrType is null) return false;
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (unwrapped == typeof(NonEmptyString) || unwrapped == typeof(Email)) return true;
        if (!unwrapped.IsGenericType) return false;
        return NumericWrapperKinds.TryGetBound(unwrapped.GetGenericTypeDefinition(), out _);
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
