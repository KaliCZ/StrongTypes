namespace StrongTypes.AspNetCore;

/// <summary>Casing applied to each segment of a normalized JSON request-body validation error key.</summary>
public enum JsonErrorKeyCasing
{
    /// <summary>
    /// Upper-case the first letter of each path segment (<c>$.value</c> → <c>Value</c>),
    /// matching the C# property name that data-annotation and model-binding errors
    /// use by default. This is the default.
    /// </summary>
    PascalCase,

    /// <summary>Lower-case the first letter of each path segment (<c>$.Value</c> → <c>value</c>), matching a camelCase JSON/validation convention.</summary>
    CamelCase,

    /// <summary>Strip the <c>$.</c> prefix only, preserving the JSON wire casing (<c>$.value</c> → <c>value</c>).</summary>
    StripOnly,
}
