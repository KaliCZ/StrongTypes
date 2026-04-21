#nullable enable

using System;

namespace StrongTypes;

public static class NullableMatchExtensions
{
    /// <summary>
    /// Invokes <paramref name="ifSome"/> with the value when present and
    /// <paramref name="ifNone"/> otherwise, returning the result of whichever
    /// branch ran. Exactly one of the two is invoked.
    /// </summary>
    public static R Match<T, R>(this T? value, Func<T, R> ifSome, Func<R> ifNone)
        where T : struct
        => value.HasValue ? ifSome(value.Value) : ifNone();

    /// <summary>
    /// Invokes <paramref name="ifSome"/> with the value when present and
    /// <paramref name="ifNone"/> otherwise, returning the result of whichever
    /// branch ran. Exactly one of the two is invoked.
    /// </summary>
    public static R Match<T, R>(this T? value, Func<T, R> ifSome, Func<R> ifNone)
        where T : class
        => value is not null ? ifSome(value) : ifNone();
}
