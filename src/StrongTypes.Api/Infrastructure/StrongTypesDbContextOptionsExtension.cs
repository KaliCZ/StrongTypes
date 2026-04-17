using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongTypes.Api.Infrastructure;

/// <summary>
/// Registers the strong-type EF Core query plugins (currently just
/// <see cref="UnwrapMethodCallTranslatorPlugin"/>) into a DbContext's internal
/// service provider. Callers wire it up with
/// <see cref="StrongTypesDbContextOptionsBuilderExtensions.UseStrongTypes"/>.
/// </summary>
public sealed class StrongTypesDbContextOptionsExtension : IDbContextOptionsExtension
{
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMethodCallTranslatorPlugin, UnwrapMethodCallTranslatorPlugin>());
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
    public static DbContextOptionsBuilder UseStrongTypes(this DbContextOptionsBuilder optionsBuilder)
    {
        var extension = optionsBuilder.Options.FindExtension<StrongTypesDbContextOptionsExtension>()
            ?? new StrongTypesDbContextOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }
}
