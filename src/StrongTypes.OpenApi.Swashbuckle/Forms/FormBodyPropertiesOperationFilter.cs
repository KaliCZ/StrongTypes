using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Repairs the form-body schema that Swashbuckle emits when every form
/// field is component-typed (e.g. <see cref="NonEmptyString"/>,
/// <see cref="Email"/>, <c>Positive&lt;int&gt;</c>). For primitive-typed
/// form fields Swashbuckle builds a proper
/// <c>{ "type": "object", "properties": { … } }</c>; when every field is
/// a complex/component type it falls back to
/// <c>{ "allOf": [ { "$ref" }, { "$ref" }, … ] }</c>, which is incoherent
/// for primitive-shaped components (the same value can't simultaneously
/// match every primitive in the list). This filter walks each form
/// <c>requestBody.content[*].schema</c>, and when the <c>allOf</c> is
/// entirely <c>$ref</c>s with one entry per form parameter it rebuilds
/// the schema as an object with a <c>properties</c> map keyed by the
/// parameter names from <see cref="OperationFilterContext.ApiDescription"/>.
/// <para>
/// Mixed forms — plain primitive fields alongside component-typed fields —
/// are left alone. Swashbuckle emits those as
/// <c>{ "allOf": [ { "$ref" }, …, { "type": "object", "properties": { plain fields } } ] }</c>
/// where the count and ordering of <c>$ref</c>s vs the trailing properties
/// bucket can't be reconstructed from <see cref="OperationFilterContext.ApiDescription"/>
/// alone. Out of scope for this filter — see the follow-up issue.
/// </para>
/// </summary>
public sealed class FormBodyPropertiesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content is not { } content) return;

        var formParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source == BindingSource.Form || p.Source == BindingSource.FormFile)
            .ToList();
        if (formParams.Count == 0) return;

        foreach (var (_, media) in content)
        {
            if (media.Schema is not OpenApiSchema schema) continue;
            if (schema.AllOf is not { Count: > 0 } allOf) continue;
            if (schema.Properties is { Count: > 0 }) continue;
            if (allOf.Count != formParams.Count) continue;
            if (!allOf.All(s => s is OpenApiSchemaReference)) continue;

            var properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
            for (var i = 0; i < formParams.Count; i++)
                properties[formParams[i].Name] = allOf[i];

            schema.AllOf = null;
            schema.Type = JsonSchemaType.Object;
            schema.Properties = properties;
        }
    }
}
