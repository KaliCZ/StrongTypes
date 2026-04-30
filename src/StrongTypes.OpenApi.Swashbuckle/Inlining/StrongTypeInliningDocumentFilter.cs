using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>
/// Runs <see cref="StrongTypeInliner"/> after every other filter so the
/// wire shape of <see cref="NonEmptyString"/> and the numeric strong-type
/// wrappers is inlined at every property/parameter/items position and
/// their components disappear from <c>components.schemas</c>. The
/// caller's data-annotations (already attached to use sites by
/// <see cref="PropertyAnnotationSchemaFilter"/>) are preserved through
/// the merge.
/// </summary>
public sealed class StrongTypeInliningDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context) => StrongTypeInliner.Inline(document);
}
