namespace StrongTypes.OpenApi.Core;

public static class StrongTypeSchemaTypes
{
    public static Type? UnwrapNullable(Type? clrType)
        => clrType is null ? null : Nullable.GetUnderlyingType(clrType) ?? clrType;

    public static bool IsInlineable(Type? clrType)
    {
        return ResolveAnnotationSurrogate(clrType) is not null
            || TryGetMaybeValue(clrType, out _);
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

    public static Type? ResolveAnnotationSurrogate(Type? clrType)
    {
        if (IsNonEmptyString(clrType) || IsEmail(clrType)) return typeof(string);
        if (IsDigit(clrType)) return typeof(int);
        if (TryGetNumeric(clrType, out var valueType, out _)) return valueType;
        if (TryGetNonEmptyEnumerableElement(clrType, out var elementType)) return typeof(IEnumerable<>).MakeGenericType(elementType);
        return null;
    }
}
