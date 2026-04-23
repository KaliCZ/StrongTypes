using FsCheck;
using FsCheck.Fluent;

namespace StrongTypes.FsCheck;

/// <summary>Shared FsCheck arbitraries for StrongTypes.</summary>
/// <remarks>Reference via <c>[Properties(Arbitrary = new[] { typeof(Generators) })]</c> on a test class.</remarks>
public static class Generators
{
    /// <summary>Arbitrary <see cref="NonEmptyString"/> values.</summary>
    public static Arbitrary<NonEmptyString> NonEmptyString { get; } =
        Arb.From(ArbMap.Default.ArbFor<string>().Generator
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.ToNonEmpty()));

    /// <summary>Arbitrary <see cref="NonEmptyString"/> or <c>null</c>, with ~5% <c>null</c>.</summary>
    public static Arbitrary<NonEmptyString?> NullableNonEmptyString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant<NonEmptyString?>(null)),
            (19, NonEmptyString.Generator.Select(nes => (NonEmptyString?)nes))));

    /// <summary>Arbitrary <see cref="Digit"/> values, uniformly over <c>0</c>–<c>9</c>.</summary>
    public static Arbitrary<Digit> Digit { get; } =
        Arb.From(Gen.Choose(0, 9).Select(n => StrongTypes.Digit.Create((char)('0' + n))));

    /// <summary>Arbitrary <see cref="Positive{T}"/> of <see cref="int"/>.</summary>
    public static Arbitrary<Positive<int>> PositiveInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i > 0)
            .Select(i => i.ToPositive()));

    /// <summary>Arbitrary <see cref="Negative{T}"/> of <see cref="int"/>.</summary>
    public static Arbitrary<Negative<int>> NegativeInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i < 0)
            .Select(i => i.ToNegative()));

    /// <summary>Arbitrary <see cref="NonNegative{T}"/> of <see cref="int"/>.</summary>
    public static Arbitrary<NonNegative<int>> NonNegativeInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i >= 0)
            .Select(i => i.ToNonNegative()));

    /// <summary>Arbitrary <see cref="NonPositive{T}"/> of <see cref="int"/>.</summary>
    public static Arbitrary<NonPositive<int>> NonPositiveInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i <= 0)
            .Select(i => i.ToNonPositive()));

    /// <summary>Arbitrary <see cref="Maybe{T}"/> of <see cref="int"/>, with ~5% <c>None</c>.</summary>
    public static Arbitrary<Maybe<int>> MaybeInt { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<int>.None)),
            (19, ArbMap.Default.ArbFor<int>().Generator.Select(Maybe.Some))));

    /// <summary>Arbitrary <see cref="Maybe{T}"/> of <see cref="string"/>, with ~5% <c>None</c>. Empty and whitespace strings remain valid <c>Some</c> values; use <see cref="MaybeNonEmptyString"/> for the non-empty invariant.</summary>
    public static Arbitrary<Maybe<string>> MaybeString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<string>.None)),
            (19, ArbMap.Default.ArbFor<string>().Generator.Select(Maybe.Some))));

    /// <summary>Arbitrary <see cref="Maybe{T}"/> of <see cref="NonEmptyString"/>, with ~5% <c>None</c>.</summary>
    public static Arbitrary<Maybe<NonEmptyString>> MaybeNonEmptyString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<NonEmptyString>.None)),
            (19, NonEmptyString.Generator.Select(Maybe.Some))));

    /// <summary>Arbitrary <see cref="Maybe{T}"/> of <see cref="Positive{T}"/> of <see cref="int"/>, with ~5% <c>None</c>.</summary>
    public static Arbitrary<Maybe<Positive<int>>> MaybePositiveInt { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<Positive<int>>.None)),
            (19, PositiveInt.Generator.Select(Maybe.Some))));

    /// <summary>Arbitrary <see cref="Maybe{T}"/> of <see cref="StrongTypes.Digit"/>, with ~5% <c>None</c>.</summary>
    public static Arbitrary<Maybe<Digit>> MaybeDigit { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<Digit>.None)),
            (19, Digit.Generator.Select(Maybe.Some))));

    /// <summary>Arbitrary <see cref="NonEmptyEnumerable{T}"/> of <see cref="int"/>.</summary>
    public static Arbitrary<NonEmptyEnumerable<int>> NonEmptyEnumerableInt { get; } =
        Arb.From(Gen.NonEmptyListOf(ArbMap.Default.ArbFor<int>().Generator)
            .Select(list => NonEmptyEnumerable.CreateRange(list)));

    /// <summary>Arbitrary <see cref="Result{T, TError}"/> of <c>int</c>/<c>string</c>, with a roughly even split between successes and errors.</summary>
    public static Arbitrary<Result<int, string>> ResultIntString { get; } =
        Arb.From(Gen.Frequency(
            (1, ArbMap.Default.ArbFor<int>().Generator.Select(i => (Result<int, string>)i)),
            (1, ArbMap.Default.ArbFor<string>().Generator.Select(s => (Result<int, string>)s))));

    /// <summary>Arbitrary <see cref="Result{T}"/> of <c>int</c>, with a roughly even split between successes and <see cref="System.InvalidOperationException"/> errors.</summary>
    public static Arbitrary<Result<int>> ResultInt { get; } =
        Arb.From(Gen.Frequency(
            (1, ArbMap.Default.ArbFor<int>().Generator.Select(i => (Result<int>)i)),
            (1, ArbMap.Default.ArbFor<string>().Generator
                .Select(s => (Result<int>)new System.InvalidOperationException(s)))));
}
