using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongTypes.EfCore;

/// <summary>
/// Wires StrongTypes into a DbContext's internal service provider:
/// <list type="bullet">
/// <item><see cref="StrongTypesConventionSetPlugin"/> auto-applies the right
/// <c>ValueConverter</c> to every strong-type property on every mapped
/// entity, running before EF's property-discovery convention so wrappers
/// never get inferred as owned entity types.</item>
/// <item><see cref="UnwrapMethodCallTranslatorPlugin"/> translates
/// <c>strongType.Unwrap()</c> inside LINQ predicates so filters like
/// <c>.Where(e =&gt; e.Name.Unwrap().Contains("foo"))</c> or
/// <c>EF.Functions.Like(e.Name.Unwrap(), "%foo%")</c> run server-side.</item>
/// </list>
/// Callers wire it up with a single
/// <see cref="StrongTypesDbContextOptionsBuilderExtensions.UseStrongTypes"/>
/// call on the options builder — no <c>ConfigureConventions</c> override
/// needed on the DbContext.
/// </summary>
public sealed class StrongTypesDbContextOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMethodCallTranslatorPlugin, UnwrapMethodCallTranslatorPlugin>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IConventionSetPlugin, StrongTypesConventionSetPlugin>());
    }

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "using StrongTypes ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) =>
            debugInfo["StrongTypes"] = "1";
    }
}

public static class StrongTypesDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="StrongTypesDbContextOptionsExtension"/> on the
    /// options builder. After this call, any strong-type property on any
    /// mapped entity gets its value converter applied automatically and
    /// <c>Unwrap()</c> inside LINQ predicates translates to server-side SQL.
    /// </summary>
    public static DbContextOptionsBuilder UseStrongTypes(this DbContextOptionsBuilder optionsBuilder)
    {
        var extension = optionsBuilder.Options.FindExtension<StrongTypesDbContextOptionsExtension>()
            ?? new StrongTypesDbContextOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
