using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Analyzers.Tests.Infrastructure;

/// <summary>
/// Curated <see cref="MetadataReference"/> sets for analyzer tests. We need fine-grained control
/// over what the test compilation "sees" — the whole point of <c>MissingEfCorePackageAnalyzer</c>
/// is to flip behavior based on which assemblies are referenced, so we cannot just dump the
/// host's loaded assembly list verbatim (it leaks EF Core and StrongTypes.EfCore into every case).
/// </summary>
internal static class TestReferences
{
    // Keep bleed-through from the host process explicit: anything matching these prefixes is a
    // test-host dependency that a user's project might not have, and tests need to control set
    // membership themselves via the `Entity*` / `StrongTypesEfCore` fields. Declared FIRST so
    // static-init order makes it available when `Core` is populated below.
    private static readonly string[] _hostOnlyPrefixes =
    {
        "Microsoft.EntityFrameworkCore",
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
