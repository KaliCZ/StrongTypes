namespace StrongTypes.AspNetCore;

/// <summary>Configures the StrongTypes ASP.NET Core MVC integration registered by <see cref="StrongTypesServiceCollectionExtensions.AddStrongTypes(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{StrongTypesAspNetCoreOptions})"/>.</summary>
public sealed class StrongTypesAspNetCoreOptions
{
    /// <summary>
    /// When <see langword="true"/> (the default), JSON request-body validation
    /// errors are reported under the bound property name (<c>value</c>) instead
    /// of the System.Text.Json path (<c>$.value</c>), matching the keys produced
    /// by model binding and data-annotation validation.
    /// </summary>
    /// <remarks>
    /// Affects only the keys in the automatic <c>ValidationProblemDetails</c>
    /// response produced for <c>[ApiController]</c> actions. It does not change
    /// System.Text.Json itself, raw <c>JsonSerializer</c> calls, or minimal-API
    /// binding, and it leaves model-binding errors (which never carry a <c>$.</c>
    /// path) untouched. Because the rewrite happens after binding, it normalizes
    /// every JSON-body error key, not only those originating from strong types.
    /// </remarks>
    public bool NormalizeJsonErrorKeys { get; set; } = true;

    /// <summary>
    /// Casing applied to normalized keys (only when <see cref="NormalizeJsonErrorKeys"/>
    /// is <see langword="true"/>). Defaults to <see cref="JsonErrorKeyCasing.PascalCase"/>,
    /// matching the C# property name that data-annotation and model-binding errors
    /// use by default; switch to <see cref="JsonErrorKeyCasing.CamelCase"/> or
    /// <see cref="JsonErrorKeyCasing.StripOnly"/> for apps whose validation keys
    /// follow the JSON name.
    /// </summary>
    public JsonErrorKeyCasing JsonErrorKeyCasing { get; set; } = JsonErrorKeyCasing.PascalCase;
}
