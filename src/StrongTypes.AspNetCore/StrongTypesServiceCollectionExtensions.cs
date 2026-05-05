using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace StrongTypes.AspNetCore;

/// <summary>Registration entry point for the StrongTypes ASP.NET Core MVC model binders.</summary>
public static class StrongTypesServiceCollectionExtensions
{
    /// <summary>Inserts the StrongTypes model binder for <see cref="NonEmptyEnumerable{T}"/> at the front of <see cref="MvcOptions.ModelBinderProviders"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static IServiceCollection AddStrongTypes(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options =>
        {
            options.ModelBinderProviders.Insert(0, new NonEmptyEnumerableModelBinderProvider());
        });
        return services;
    }
}
