namespace StrongTypes.OpenApi.Core;

public static class StrongTypeSchemaTypes
{
    public static bool IsInlineable(Type? clrType)
    {
        if (clrType is null) return false;

        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (unwrapped == typeof(NonEmptyString) || unwrapped == typeof(Email) || unwrapped == typeof(Digit)) return true;
        if (!unwrapped.IsGenericType) return false;

        var definition = unwrapped.GetGenericTypeDefinition();
        return NumericWrapperKinds.TryGetBound(definition, out _)
            || definition == typeof(NonEmptyEnumerable<>)
            || definition == typeof(INonEmptyEnumerable<>)
            || definition == typeof(Maybe<>);
    }
}
