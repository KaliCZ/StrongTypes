using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Runs <see cref="StrongTypeInliner"/> after every other transformer so
/// the wire shape of <see cref="NonEmptyString"/> and the numeric strong-
/// type wrappers is inlined at every property/parameter/items position
/// and their components disappear from <c>components.schemas</c>. The
/// caller's data-annotations (already attached to use sites by
/// <see cref="PropertyAnnotationSchemaTransformer"/>) are preserved
/// through the merge.
/// </summary>
internal sealed class StrongTypeInliningDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        StrongTypeInliner.Inline(document);
        return Task.CompletedTask;
    }
}
