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
    /// <see cref="NonEmptyString"/> with ~10% chance of <c>null</c>. Tuned
    /// so FsCheck's default 100-case run exercises the null branch several
    /// times while keeping the majority of cases on the happy path.
    /// </summary>
    public static Arbitrary<NonEmptyString?> NullableNonEmptyString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant<NonEmptyString?>(null)),
            (9, ArbMap.Default.ArbFor<string>().Generator
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => (NonEmptyString?)NonEmptyString.Create(s)))));

    /// <summary>
    /// <see cref="Maybe{Int32}"/> with ~20% chance of <see cref="Maybe{T}.Empty"/>.
    /// Higher empty rate than NullableNonEmptyString because Maybe's equality and
    /// comparison logic has distinct empty/populated branches that benefit from
    /// denser coverage of the empty case.
    /// </summary>
    public static Arbitrary<Maybe<int>> MaybeInt { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<int>.None)),
            (4, ArbMap.Default.ArbFor<int>().Generator.Select(Maybe<int>.Some))));

    /// <summary>
    /// <see cref="Maybe{String}"/> with ~20% chance of <see cref="Maybe{T}.Empty"/>.
    /// Generated strings are constrained to non-empty / non-whitespace so the value
    /// carries information distinguishable from the empty case when asserting.
    /// </summary>
    public static Arbitrary<Maybe<string>> MaybeString { get; } =
        Arb.From(Gen.Frequency(
            (1, Gen.Constant(Maybe<string>.None)),
            (4, ArbMap.Default.ArbFor<string>().Generator
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(Maybe<string>.Some))));
}
