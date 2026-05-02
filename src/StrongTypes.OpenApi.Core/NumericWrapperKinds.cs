namespace StrongTypes.OpenApi.Core;

/// <summary>
/// A single numeric bound: an inclusive or exclusive endpoint plus a flag
/// for which side it sits on.
/// </summary>
public readonly record struct NumericBound(decimal Value, bool Exclusive, bool IsLower);

/// <summary>
/// One row per numeric strong-type wrapper, pairing its CLR generic
/// definition with the numeric bound it imposes.
/// </summary>
public sealed record NumericWrapperKind(Type GenericDefinition, NumericBound Bound);

/// <summary>
/// Single source of truth for the four numeric wrappers
/// (<see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>,
/// <see cref="Negative{T}"/>, <see cref="NonPositive{T}"/>) — used by the
/// schema-time painters in both pipelines to dispatch on the CLR generic
/// definition.
/// </summary>
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
