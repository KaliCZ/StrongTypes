using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

/// <summary>
/// Curated <see cref="MetadataReference"/> sets for analyzer tests: dumping the host's loaded
/// assemblies would leak EF Core and StrongTypes.EfCore into every case and defeat the
/// reference-sensitive analyzers.
/// </summary>
internal static class TestReferences
{
    // Must be declared before Core: Core's initializer reads this.
    private static readonly string[] _hostOnlyPrefixes =
    {
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore.OpenApi",
        "Swashbuckle",
        "StrongTypes",
        "Kalicz.StrongTypes",
    };

    public static readonly IReadOnlyList<MetadataReference> Core = BuildCoreReferences();

    public static readonly MetadataReference StrongTypes =
        MetadataReference.CreateFromFile(typeof(NonEmptyString).Assembly.Location);

    public static readonly MetadataReference EntityFrameworkCore =
        MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location);

    public static readonly MetadataReference StrongTypesEfCore =
        MetadataReference.CreateFromFile(typeof(global::StrongTypes.EfCore.StrongTypesDbContextOptionsExtension).Assembly.Location);

    public static readonly MetadataReference MicrosoftAspNetCoreOpenApi =
        MetadataReference.CreateFromFile(typeof(global::Microsoft.AspNetCore.OpenApi.IOpenApiSchemaTransformer).Assembly.Location);

    public static readonly MetadataReference SwashbuckleAspNetCoreSwaggerGen =
        MetadataReference.CreateFromFile(typeof(global::Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter).Assembly.Location);

    public static readonly MetadataReference StrongTypesOpenApiMicrosoft =
        MetadataReference.CreateFromFile(typeof(global::StrongTypes.OpenApi.Microsoft.StrongTypesOpenApiExtensions).Assembly.Location);

    public static readonly MetadataReference StrongTypesOpenApiSwashbuckle =
        MetadataReference.CreateFromFile(typeof(global::StrongTypes.OpenApi.Swashbuckle.StrongTypesSwashbuckleExtensions).Assembly.Location);

    public static readonly MetadataReference StrongTypesConfiguration =
        MetadataReference.CreateFromFile(typeof(global::StrongTypes.Configuration.OptionsBuilderExtensions).Assembly.Location);

    /// <summary>What an options-binding source needs to compile. <c>[TypeConverter]</c> is included
    /// because ST0004 reads it to know where the binder stops recursing.</summary>
    public static readonly MetadataReference[] OptionsStack =
    [
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.Options.IOptions<object>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location),
        // Two assemblies: the attribute lives in System.ComponentModel.Primitives, the TypeConverter
        // base class a source needs to subclass is forwarded to System.ComponentModel.TypeConverter.
        MetadataReference.CreateFromFile(typeof(global::System.ComponentModel.TypeConverterAttribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::System.ComponentModel.TypeConverter).Assembly.Location),
    ];

    private static IReadOnlyList<MetadataReference> BuildCoreReferences()
    {
        // AppContext "TRUSTED_PLATFORM_ASSEMBLIES" isn't always populated under
        // Microsoft.Testing.Platform hosts, so enumerate the loaded set instead. Force-load a few
        // BCL assemblies the analyzer's test sources depend on so they're present in
        // `AppDomain.CurrentDomain.GetAssemblies()` by the time we filter.
        _ = typeof(System.Linq.Enumerable).FullName;
        _ = typeof(System.Collections.Generic.List<int>).FullName;
        _ = typeof(System.Runtime.CompilerServices.RequiredMemberAttribute).FullName;

        var references = new List<MetadataReference>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
            {
                continue;
            }
            var name = assembly.GetName().Name ?? string.Empty;
            if (_hostOnlyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            {
                continue;
            }
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }
        return references;
    }

    public static IEnumerable<MetadataReference> With(params MetadataReference[] extras)
    {
        foreach (var r in Core)
        {
            yield return r;
        }
        yield return StrongTypes;
        foreach (var extra in extras)
        {
            yield return extra;
        }
    }
}
