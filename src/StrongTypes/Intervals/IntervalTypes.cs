#nullable enable
using System;

namespace StrongTypes;

/// <summary>Reflection helpers for the four interval types: <see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, and <see cref="IntervalUntil{T}"/>.</summary>
public static class IntervalTypes
{
    /// <summary>Whether <paramref name="type"/> — or, when it is <see cref="Nullable{T}"/>, its underlying type — is one of the four interval types.</summary>
    public static bool IsInterval(Type type) => TryGetEndpoints(type, out _, out _);

    /// <summary>Resolves an interval's <c>Start</c> and <c>End</c> endpoint types — the bare endpoint type for a required endpoint, its <see cref="Nullable{T}"/> for an optional one — or returns <see langword="false"/> when <paramref name="type"/> is not an interval.</summary>
    public static bool TryGetEndpoints(Type type, out Type startType, out Type endType)
    {
        startType = null!;
        endType = null!;
        var unwrapped = Nullable.GetUnderlyingType(type) ?? type;
        if (!unwrapped.IsGenericType)
        {
            return false;
        }
        var definition = unwrapped.GetGenericTypeDefinition();
        var endpoint = unwrapped.GetGenericArguments()[0];
        Type Optional() => typeof(Nullable<>).MakeGenericType(endpoint);
        if (definition == typeof(FiniteInterval<>)) { startType = endpoint; endType = endpoint; return true; }
        if (definition == typeof(Interval<>)) { startType = Optional(); endType = Optional(); return true; }
        if (definition == typeof(IntervalFrom<>)) { startType = endpoint; endType = Optional(); return true; }
        if (definition == typeof(IntervalUntil<>)) { startType = Optional(); endType = endpoint; return true; }
        return false;
    }
}
