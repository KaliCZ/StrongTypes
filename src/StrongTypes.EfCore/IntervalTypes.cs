namespace StrongTypes.EfCore;

internal static class IntervalTypes
{
    public static bool IsInterval(Type clrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (!unwrapped.IsGenericType)
        {
            return false;
        }
        var definition = unwrapped.GetGenericTypeDefinition();
        return definition == typeof(FiniteInterval<>)
            || definition == typeof(Interval<>)
            || definition == typeof(IntervalFrom<>)
            || definition == typeof(IntervalUntil<>);
    }
}
