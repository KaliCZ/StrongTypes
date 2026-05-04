using System;
using System.Globalization;
using System.Reflection;

namespace StrongTypes.AspNetCore;

/// <summary>Parses a single wire string into <typeparamref name="T"/> via <see cref="IParsable{TSelf}"/>.</summary>
/// <remarks>Discovered by reflection once per closed type. Works for any <typeparamref name="T"/> with a public static <c>TryParse(string, IFormatProvider?, out T)</c> — every BCL primitive in net7+ and every Kalicz.StrongTypes wrapper.</remarks>
internal static class StringElementParser<T>
{
    private delegate bool TryParseDelegate(string? s, IFormatProvider? provider, out T result);

    private static readonly TryParseDelegate? s_tryParse = ResolveTryParse();

    /// <summary>True when <typeparamref name="T"/> exposes the <c>IParsable</c> shape; false otherwise (no parser available — the caller surfaces a binding error).</summary>
    public static bool IsSupported => s_tryParse is not null;

    public static bool TryParse(string value, out T result)
    {
        if (s_tryParse is null)
        {
            result = default!;
            return false;
        }
        return s_tryParse(value, CultureInfo.InvariantCulture, out result);
    }

    private static TryParseDelegate? ResolveTryParse()
    {
        var method = typeof(T).GetMethod(
            name: "TryParse",
            bindingAttr: BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(string), typeof(IFormatProvider), typeof(T).MakeByRefType()],
            modifiers: null);

        if (method is null) return null;

        return (TryParseDelegate)Delegate.CreateDelegate(typeof(TryParseDelegate), method);
    }
}
