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
/// Layers caller-supplied data-annotations onto strong-type wrappers at non-body slots —
/// <c>[FromQuery]</c>/<c>[FromRoute]</c>/<c>[FromHeader]</c> parameters and <c>[FromForm]</c>
/// fields — which <see cref="PropertyAnnotationSchemaFilter"/> never reaches. Also reshapes
/// the <c>{ allOf: [&lt;each-field&gt;] }</c> form body Swashbuckle emits for component-typed
/// fields into a <c>{ type: object, properties: { … } }</c> map keyed by field name.
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

        ReshapeFormAllOfIntoProperties(operation, descriptions, context.SchemaGenerator, context.SchemaRepository, logger);

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

    private static void ReshapeFormAllOfIntoProperties(
        OpenApiOperation operation,
        IList<ApiParameterDescription> descriptions,
        ISchemaGenerator schemaGenerator,
        SchemaRepository schemaRepository,
        ILogger? logger)
    {
        if (operation.RequestBody?.Content is not { } content) return;

        foreach (var contentType in s_formContentTypes)
        {
            if (!content.TryGetValue(contentType, out var media)) continue;
            if (media.Schema is not OpenApiSchema formSchema) continue;
            if (formSchema.AllOf is not { Count: > 0 } allOf) continue;
            if (formSchema.Properties is { Count: > 0 }) continue;

            var formParamCount = 0;
            var properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
            foreach (var pd in descriptions)
            {
                if (pd.Source != BindingSource.Form) continue;
                formParamCount++;

                var clrType = ResolveParameterClrType(pd);

                // MemberInfo makes Swashbuckle apply caller annotations itself — right for primitives;
                // wrapper slots get theirs from MergeFormPropertyAnnotations, so a copy here would double-emit.
                MemberInfo? memberInfo = null;
                if (!StrongTypeSchemaTypes.IsInlineable(clrType))
                    memberInfo = ResolveFormPropertyMember(pd);

                properties[pd.Name] = schemaGenerator.GenerateSchema(clrType, schemaRepository, memberInfo);
            }

            if (properties.Count == 0) continue;

            if (properties.Count != formParamCount)
            {
                logger?.LogError(
                    "StrongTypes form-body reshape on operation '{OperationId}' resolved {ResolvedCount} of {TotalCount} form parameters; the rest will be missing from the emitted properties map.",
                    operation.OperationId ?? "(unnamed)", properties.Count, formParamCount);
            }

            if (allOf.Count > formParamCount)
            {
                logger?.LogError(
                    "StrongTypes form-body reshape on operation '{OperationId}' replaced an allOf with {AllOfCount} entries using only {FormParamCount} form parameters; entries beyond the parameter set are dropped. This usually means Swashbuckle assembled the form body in a shape we don't recognise.",
                    operation.OperationId ?? "(unnamed)", allOf.Count, formParamCount);
            }

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

        if (slot is OpenApiSchema concrete)
        {
            WrapperAnnotationApplier.TryApply(concrete, clrType, attributes);
            return concrete;
        }

        // A $ref slot can't carry annotations directly; wrap it in allOf + marker so StrongTypeInliner collapses the pair later.
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

    // Not pd.Type: it lies for IParsable<T> strong types, reporting the string overload's input instead of the wrapper.
    private static Type ResolveParameterClrType(ApiParameterDescription pd) => pd.ModelMetadata.ModelType;
}
