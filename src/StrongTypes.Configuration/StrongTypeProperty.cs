using System.Reflection;

namespace StrongTypes.Configuration;

internal static class StrongTypeProperty
{
    private static readonly HashSet<Type> StrongTypeDefinitions =
    [
        typeof(NonEmptyString),
        typeof(Email),
        typeof(Digit),
        typeof(Positive<>),
        typeof(NonNegative<>),
        typeof(Negative<>),
        typeof(NonPositive<>),
    ];

    public static bool IsStrongType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return StrongTypeDefinitions.Contains(underlying.IsGenericType ? underlying.GetGenericTypeDefinition() : underlying);
    }

    /// <summary>
    /// True when the declaration says the value may be absent: <c>Positive&lt;int&gt;?</c>, or a
    /// reference wrapper the assembly annotated as nullable.
    /// </summary>
    /// <remarks>
    /// An assembly compiled without nullable reference types carries no annotation, so a reference
    /// wrapper reads as <see cref="NullabilityState.Unknown"/>. That is treated as optional: with
    /// nothing declared there is no intent to enforce, and failing would make the package unusable
    /// for those projects.
    /// </remarks>
    public static bool IsOptional(PropertyInfo property, Func<PropertyInfo, NullabilityInfo> nullability)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }
        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        return nullability(property).WriteState != NullabilityState.NotNull;
    }
}
