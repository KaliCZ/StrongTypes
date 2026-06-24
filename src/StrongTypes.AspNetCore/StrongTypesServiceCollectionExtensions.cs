using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace StrongTypes.AspNetCore;

/// <summary>Registration entry point for the StrongTypes ASP.NET Core MVC integration.</summary>
public static class StrongTypesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the StrongTypes MVC model binder for <see cref="NonEmptyEnumerable{T}"/>
    /// and, unless turned off via <paramref name="configure"/>, normalizes JSON
    /// request-body validation error keys (see <see cref="StrongTypesAspNetCoreOptions.NormalizeJsonErrorKeys"/>).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional configuration callback.</param>
    public static IServiceCollection AddStrongTypes(
        this IServiceCollection services,
        Action<StrongTypesAspNetCoreOptions>? configure = null)
    {
        var options = new StrongTypesAspNetCoreOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.Configure<MvcOptions>(mvc =>
        {
            mvc.ModelBinderProviders.Insert(0, new NonEmptyEnumerableModelBinderProvider());
        });

        // Wrap the default factory and rewrite the keys at request time, reading
        // the flag from DI so tests (and consumers) can flip it per app instance.
        // PostConfigure runs after the framework's own Configure that installs the
        // default factory, so wrapping it doesn't depend on AddStrongTypes being
        // called after AddControllers.
        services.PostConfigure<ApiBehaviorOptions>(api =>
        {
            var inner = api.InvalidModelStateResponseFactory;
            api.InvalidModelStateResponseFactory = context =>
            {
                var resolved = context.HttpContext.RequestServices.GetRequiredService<StrongTypesAspNetCoreOptions>();
                if (!resolved.NormalizeJsonErrorKeys) return inner(context);

                var normalized = NormalizeKeys(context.ModelState, resolved.JsonErrorKeyCasing);
                var rewritten = new ActionContext(
                    context.HttpContext, context.RouteData, context.ActionDescriptor, normalized);
                return inner(rewritten);
            };
        });

        return services;
    }

    private static ModelStateDictionary NormalizeKeys(ModelStateDictionary source, JsonErrorKeyCasing casing)
    {
        var result = new ModelStateDictionary();
        foreach (var (key, entry) in source)
        {
            var normalizedKey = JsonValidationErrorKeyNormalizer.Normalize(key, casing);
            foreach (var error in entry.Errors)
            {
                if (error.Exception is not null && error.ErrorMessage.Length == 0)
                {
                    result.TryAddModelException(normalizedKey, error.Exception);
                }
                else
                {
                    result.TryAddModelError(normalizedKey, error.ErrorMessage);
                }
            }
        }

        return result;
    }
}
