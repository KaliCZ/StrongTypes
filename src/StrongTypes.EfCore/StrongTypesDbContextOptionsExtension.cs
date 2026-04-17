using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongTypes.EfCore;

/// <summary>
/// Wires the <see cref="UnwrapMethodCallTranslatorPlugin"/> into a DbContext's
/// internal service provider so <c>strongType.Unwrap()</c> inside LINQ
/// predicates translates to server-side SQL (e.g.
/// <c>.Where(e =&gt; e.Name.Unwrap().Contains("foo"))</c> or
/// <c>EF.Functions.Like(e.Name.Unwrap(), "%foo%")</c>).
/// </summary>
/// <remarks>
/// Pair with <c>ModelConfigurationBuilder.UseStrongTypes()</c> in your
/// DbContext's <c>ConfigureConventions</c> override — that registers the
/// value converters, which have to go in EF Core's pre-convention phase
/// (before property discovery) and can't be registered from DI.
/// </remarks>
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
