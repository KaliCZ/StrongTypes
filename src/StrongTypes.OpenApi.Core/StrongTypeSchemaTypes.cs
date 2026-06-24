using System.Globalization;
using System.Reflection;

namespace StrongTypes.OpenApi.Core;

public static class StrongTypeSchemaTypes
{
    public static Type? UnwrapNullable(Type? clrType)
        => clrType is null ? null : Nullable.GetUnderlyingType(clrType) ?? clrType;

    public static bool IsInlineable(Type? clrType)
    {
        return ResolveWireType(clrType) is not null
            || TryGetMaybeValue(clrType, out _);
    }

    public static bool IsNonEmptyString(Type? clrType)
        => UnwrapNullable(clrType) == typeof(NonEmptyString);

    public static bool IsEmail(Type? clrType)
        => UnwrapNullable(clrType) == typeof(Email);

    public static bool IsDigit(Type? clrType)
        => UnwrapNullable(clrType) == typeof(Digit);

    public static bool IsBoundedInt(Type? clrType)
    {
        var unwrapped = UnwrapNullable(clrType);
        return unwrapped is { IsGenericType: true }
            && unwrapped.GetGenericTypeDefinition() == typeof(BoundedInt<>);
    }

    /// <summary>
    /// Reads the underlying primitive and the inclusive <c>[Min, Max]</c>
    /// range of a <see cref="BoundedInt{TBounds}"/>. Unlike the four
    /// single-bound numeric wrappers, the range is carried by the witness
    /// type, so it's read off the closed wrapper's static <c>Min</c>/<c>Max</c>
    /// at schema-generation time rather than from a static table.
    /// </summary>
    public static bool TryGetBoundedInt(Type? clrType, out Type valueType, out decimal min, out decimal max)
    {
        valueType = null!;
        min = default;
        max = default;

        var unwrapped = UnwrapNullable(clrType);
        if (unwrapped is not { IsGenericType: true } || unwrapped.GetGenericTypeDefinition() != typeof(BoundedInt<>))
            return false;

        valueType = unwrapped.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!.PropertyType;
        min = Convert.ToDecimal(unwrapped.GetProperty("Min", BindingFlags.Public | BindingFlags.Static)!.GetValue(null), CultureInfo.InvariantCulture);
        max = Convert.ToDecimal(unwrapped.GetProperty("Max", BindingFlags.Public | BindingFlags.Static)!.GetValue(null), CultureInfo.InvariantCulture);
        return true;
    }

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

    public static Type? ResolveWireType(Type? clrType)
    {
        if (IsNonEmptyString(clrType) || IsEmail(clrType)) return typeof(string);
        if (IsDigit(clrType)) return typeof(int);
        if (TryGetBoundedInt(clrType, out var boundedValueType, out _, out _)) return boundedValueType;
        if (TryGetNumeric(clrType, out var valueType, out _)) return valueType;
        if (TryGetNonEmptyEnumerableElement(clrType, out var elementType)) return typeof(IEnumerable<>).MakeGenericType(elementType);
        return null;
    }
}
