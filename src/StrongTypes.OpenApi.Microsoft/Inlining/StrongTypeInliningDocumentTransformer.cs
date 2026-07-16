using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

/// <summary>
/// Runs <see cref="StrongTypeInliner"/>; must run after every other transformer so use-site annotations are already attached.
/// </summary>
internal sealed class StrongTypeInliningDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var logger = context.ApplicationServices
            .GetService<ILoggerFactory>()
            ?.CreateLogger<StrongTypeInliningDocumentTransformer>();
        StrongTypeInliner.Inline(document, logger);
        return Task.CompletedTask;
    }
}
