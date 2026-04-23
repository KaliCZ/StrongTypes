#nullable enable

using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A numeric value guaranteed to be less than or equal to <c>T.Zero</c>.</summary>
/// <typeparam name="T">The underlying numeric type.</typeparam>
/// <remarks>Construct via <see cref="TryCreate"/> or <c>Create</c>. <c>default(NonPositive&lt;T&gt;)</c> wraps <c>T.Zero</c> and satisfies the invariant.</remarks>
[NumericWrapper(InvariantDescription = "non-positive", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct NonPositive<T>
    where T : INumber<T>
{
    private NonPositive(T value)
    {
        Value = value;
    }

    public T Value { get; }

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is greater than zero.</summary>
    /// <param name="value">The number to validate.</param>
    public static NonPositive<T>? TryCreate(T value)
    {
        return value <= T.Zero ? new NonPositive<T>(value) : null;
    }
}
