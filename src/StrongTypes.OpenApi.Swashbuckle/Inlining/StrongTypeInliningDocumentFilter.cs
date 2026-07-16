using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StrongTypes.OpenApi.Swashbuckle;

/// <summary>Runs <see cref="StrongTypeInliner"/>; must run after every other filter so use-site annotations are already attached.</summary>
public sealed class StrongTypeInliningDocumentFilter(ILogger<StrongTypeInliningDocumentFilter>? logger = null) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context) => StrongTypeInliner.Inline(document, logger);
}
