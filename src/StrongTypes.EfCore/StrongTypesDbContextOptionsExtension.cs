using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongTypes.EfCore;

/// <summary>Wires StrongTypes value conversions and LINQ translations into a DbContext.</summary>
/// <remarks>Enable with <see cref="StrongTypesDbContextOptionsBuilderExtensions.UseStrongTypes"/> on the options builder; no <c>ConfigureConventions</c> override required.</remarks>
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
    /// <summary>Enables automatic value conversion of strong-type properties and server-side translation of <c>Unwrap()</c> in LINQ predicates.</summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    public static DbContextOptionsBuilder UseStrongTypes(this DbContextOptionsBuilder optionsBuilder)
    {
        var extension = optionsBuilder.Options.FindExtension<StrongTypesDbContextOptionsExtension>()
            ?? new StrongTypesDbContextOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
