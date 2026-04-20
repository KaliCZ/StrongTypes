#nullable enable

using FsCheck;
using FsCheck.Fluent;

namespace StrongTypes.Tests;

/// <summary>
/// Shared FsCheck arbitraries for the test project. Reference via
/// <c>[Properties(Arbitrary = new[] { typeof(Generators) })]</c>
/// on a test class. Add new arbitraries here rather than creating
/// per-feature generator classes.
/// </summary>
public static class Generators
{
    /// <summary>
    /// <see cref="NonEmptyString"/>. FsCheck's default string generator can
    /// produce empty/whitespace values; we filter those out so the generated
    /// value always satisfies the type's invariant.
    /// </summary>
    public static Arbitrary<NonEmptyString> NonEmptyString { get; } =
        Arb.From(ArbMap.Default.ArbFor<string>().Generator
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(StrongTypes.NonEmptyString.Create));

    /// <summary>
    /// <see cref="NonEmptyString"/> with ~10% chance of <c>null</c>. Tuned
    /// so FsCheck's default 100-case run exercises the null branch several
    /// times while keeping the majority of cases on the happy path.
    /// </summary>
    public static Arbitrary<NonEmptyString?> NullableNonEmptyString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant<NonEmptyString?>(null)),
            (9, ArbMap.Default.ArbFor<string>().Generator
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => (NonEmptyString?)StrongTypes.NonEmptyString.Create(s)))));

    /// <summary>
    /// <see cref="Positive{Int32}"/> — values strictly greater than zero.
    /// </summary>
    public static Arbitrary<Positive<int>> PositiveInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i > 0)
            .Select(Positive<int>.Create));

    /// <summary>
    /// <see cref="Negative{Int32}"/> — values strictly less than zero.
    /// </summary>
    public static Arbitrary<Negative<int>> NegativeInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i < 0)
            .Select(Negative<int>.Create));

    /// <summary>
    /// <see cref="NonNegative{Int32}"/> — values greater than or equal to zero.
    /// </summary>
    public static Arbitrary<NonNegative<int>> NonNegativeInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i >= 0)
            .Select(NonNegative<int>.Create));

    /// <summary>
    /// <see cref="NonPositive{Int32}"/> — values less than or equal to zero.
    /// </summary>
    public static Arbitrary<NonPositive<int>> NonPositiveInt { get; } =
        Arb.From(ArbMap.Default.ArbFor<int>().Generator
            .Where(i => i <= 0)
            .Select(NonPositive<int>.Create));

    /// <summary>
    /// <see cref="Maybe{Int32}"/> with ~20% chance of <see cref="Maybe{T}.None"/>.
    /// Higher None rate than NullableNonEmptyString because Maybe's equality and
    /// comparison logic has distinct None/Some branches that benefit from
    /// denser coverage of the None case.
    /// </summary>
    public static Arbitrary<Maybe<int>> MaybeInt { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<int>.None)),
            (4, ArbMap.Default.ArbFor<int>().Generator.Select(Maybe<int>.Some))));

    /// <summary>
    /// <see cref="Maybe{String}"/> with ~20% chance of <see cref="Maybe{T}.None"/>.
    /// Only <c>null</c> from the underlying generator collapses to None — empty
    /// and whitespace strings are valid <c>Some</c> values, since <c>string</c>
    /// itself doesn't forbid them. Use <see cref="MaybeNonEmptyString"/> when
    /// you want the non-empty invariant.
    /// </summary>
    public static Arbitrary<Maybe<string>> MaybeString { get; } =
        Arb.From(ArbMap.Default.ArbFor<string>().Generator
            .Select(s => s is null ? Maybe<string>.None : Maybe<string>.Some(s)));

    /// <summary>
    /// <see cref="Maybe{T}"/> of <see cref="NonEmptyString"/> with ~20% None rate.
    /// </summary>
    public static Arbitrary<Maybe<NonEmptyString>> MaybeNonEmptyString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<NonEmptyString>.None)),
            (4, ArbMap.Default.ArbFor<string>().Generator
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => Maybe<NonEmptyString>.Some(StrongTypes.NonEmptyString.Create(s))))));

    /// <summary>
    /// <see cref="Maybe{T}"/> of <see cref="Positive{Int32}"/> with ~20% None rate.
    /// The wrapped int is constrained to <c>&gt; 0</c> to satisfy the Positive
    /// invariant, so generated values always round-trip cleanly through the
    /// strong-type converter.
    /// </summary>
    public static Arbitrary<Maybe<Positive<int>>> MaybePositiveInt { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<Positive<int>>.None)),
            (4, ArbMap.Default.ArbFor<int>().Generator
                .Where(i => i > 0)
                .Select(i => Maybe<Positive<int>>.Some(Positive<int>.Create(i))))));
}
