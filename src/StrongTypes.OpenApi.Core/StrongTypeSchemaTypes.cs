namespace StrongTypes.OpenApi.Core;

public static class StrongTypeSchemaTypes
{
    public static Type? UnwrapNullable(Type? clrType)
        => clrType is null ? null : Nullable.GetUnderlyingType(clrType) ?? clrType;

    public static bool IsInlineable(Type? clrType)
    {
        return ResolveWireType(clrType) is not null
            || TryGetMaybeValue(clrType, out _)
            || TryGetIntervalEndpoints(clrType, out _, out _);
    }

    public static bool IsNonEmptyString(Type? clrType)
        => UnwrapNullable(clrType) == typeof(NonEmptyString);

    public static bool IsEmail(Type? clrType)
        => UnwrapNullable(clrType) == typeof(Email);

    public static bool IsDigit(Type? clrType)
        => UnwrapNullable(clrType) == typeof(Digit);

    public static bool TryGetNumeric(Type? clrType, out Type valueType, out NumericBound bound)
    {
        valueType = null!;
        bound = default;

        var unwrapped = UnwrapNullable(clrType);
        if (unwrapped is null || !unwrapped.IsGenericType) return false;
        var definition = unwrapped.GetGenericTypeDefinition();
        if (!NumericWrapperKinds.TryGetBound(definition, out bound)) return false;

        valueType = unwrapped.GetGenericArguments()[0];
        return true;
    }

    public static bool TryGetNonEmptyEnumerableElement(Type? clrType, out Type elementType)
    {
        elementType = null!;

        var unwrapped = UnwrapNullable(clrType);
        if (unwrapped is null || !unwrapped.IsGenericType) return false;
        var definition = unwrapped.GetGenericTypeDefinition();
        if (definition != typeof(NonEmptyEnumerable<>) && definition != typeof(INonEmptyEnumerable<>)) return false;

        elementType = unwrapped.GetGenericArguments()[0];
        return true;
    }

    public static bool TryGetMaybeValue(Type? clrType, out Type valueType)
    {
        valueType = null!;

        var unwrapped = UnwrapNullable(clrType);
        if (unwrapped is null || !unwrapped.IsGenericType) return false;
        if (unwrapped.GetGenericTypeDefinition() != typeof(Maybe<>)) return false;

        valueType = unwrapped.GetGenericArguments()[0];
        return true;
    }

    /// <summary>
    /// Resolves the CLR types of an interval's <c>Start</c> and <c>End</c>
    /// endpoints, reflecting each variant's nullability: a required endpoint is
    /// the bare endpoint type, an optional one its <see cref="Nullable{T}"/>.
    /// Both keys are always present on the wire regardless of nullability.
    /// </summary>
    public static bool TryGetIntervalEndpoints(Type? clrType, out Type startType, out Type endType)
    {
        startType = null!;
        endType = null!;

        var unwrapped = UnwrapNullable(clrType);
        if (unwrapped is null || !unwrapped.IsGenericType) return false;
        var definition = unwrapped.GetGenericTypeDefinition();

        // The endpoint is a struct for every interval type, so Nullable<endpoint>
        // is only well-formed once we know it's an interval — build it lazily.
        var endpoint = unwrapped.GetGenericArguments()[0];
        Type Nullable() => typeof(Nullable<>).MakeGenericType(endpoint);

        if (definition == typeof(ClosedInterval<>)) { startType = endpoint; endType = endpoint; return true; }
        if (definition == typeof(Interval<>)) { startType = Nullable(); endType = Nullable(); return true; }
        if (definition == typeof(IntervalFrom<>)) { startType = endpoint; endType = Nullable(); return true; }
        if (definition == typeof(IntervalUntil<>)) { startType = Nullable(); endType = endpoint; return true; }
        return false;
    }

    public static Type? ResolveWireType(Type? clrType)
    {
        if (IsNonEmptyString(clrType) || IsEmail(clrType)) return typeof(string);
        if (IsDigit(clrType)) return typeof(int);
        if (TryGetNumeric(clrType, out var valueType, out _)) return valueType;
        if (TryGetNonEmptyEnumerableElement(clrType, out var elementType)) return typeof(IEnumerable<>).MakeGenericType(elementType);
        return null;
    }
}
