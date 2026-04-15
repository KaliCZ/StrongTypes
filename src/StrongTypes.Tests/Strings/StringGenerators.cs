#nullable enable

using FsCheck;
using FsCheck.Fluent;

namespace StrongTypes.Tests;

/// <summary>
/// Shared FsCheck arbitraries for string-shaped values. Reference via
/// <c>[Properties(Arbitrary = new[] { typeof(StringGenerators) })]</c>
/// on a test class.
/// </summary>
public static class StringGenerators
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
}
