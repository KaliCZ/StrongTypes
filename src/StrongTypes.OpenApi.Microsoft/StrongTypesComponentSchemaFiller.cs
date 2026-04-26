using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Microsoft;

// The framework's schema-deduplication pass copies repeated wrapper schemas
// (Maybe<T>, Positive<T>, …) into components.schemas with a fresh
// OpenApiSchema instance — that copy doesn't pick up our schema-transformer
// mutations on certain wrapper types, leaving the component as `{}`. This
// document transformer runs after deduplication and re-applies the wire
// contract to each component schema by name, so referenced schemas match the
// inline ones.
internal sealed class StrongTypesComponentSchemaFiller : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is not { } schemas) return Task.CompletedTask;

        foreach (var (name, sch) in schemas)
        {
            if (sch is not OpenApiSchema concrete) continue;
            if (StrongTypeSchemaPainter.TryPaint(name, concrete, schemas))
            {
                // Painted in place.
            }
        }

        return Task.CompletedTask;
    }
}
