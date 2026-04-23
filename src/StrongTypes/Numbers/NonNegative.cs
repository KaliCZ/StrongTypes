using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A numeric value guaranteed to be greater than or equal to <c>T.Zero</c>.</summary>
/// <typeparam name="T">The underlying numeric type.</typeparam>
/// <remarks>Construct via <see cref="TryCreate"/> or <c>Create</c>. <c>default(NonNegative&lt;T&gt;)</c> wraps <c>T.Zero</c> and satisfies the invariant.</remarks>
[NumericWrapper(InvariantDescription = "non-negative", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct NonNegative<T>
    where T : INumber<T>
{
    private NonNegative(T value)
    {
        Value = value;
    }

    [Pure]
    public T Value { get; }

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is less than zero.</summary>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static NonNegative<T>? TryCreate(T value)
    {
        return value >= T.Zero ? new NonNegative<T>(value) : null;
    }
}
