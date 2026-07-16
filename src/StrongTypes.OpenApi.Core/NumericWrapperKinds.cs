namespace StrongTypes.OpenApi.Core;

public readonly record struct NumericBound(decimal Value, bool Exclusive, bool IsLower);

public sealed record NumericWrapperKind(Type GenericDefinition, NumericBound Bound);

/// <summary>Single source of truth for the numeric strong-type wrappers and the bound each imposes.</summary>
public static class NumericWrapperKinds
{
    public static readonly IReadOnlyList<NumericWrapperKind> All =
    [
        new(typeof(Positive<>),    new NumericBound(Value: 0m, Exclusive: true,  IsLower: true)),
        new(typeof(NonNegative<>), new NumericBound(Value: 0m, Exclusive: false, IsLower: true)),
        new(typeof(Negative<>),    new NumericBound(Value: 0m, Exclusive: true,  IsLower: false)),
        new(typeof(NonPositive<>), new NumericBound(Value: 0m, Exclusive: false, IsLower: false)),
    ];

    private static readonly Dictionary<Type, NumericBound> s_boundByDefinition = All.ToDictionary(k => k.GenericDefinition, k => k.Bound);

    public static bool TryGetBound(Type genericDefinition, out NumericBound bound) => s_boundByDefinition.TryGetValue(genericDefinition, out bound);
}
