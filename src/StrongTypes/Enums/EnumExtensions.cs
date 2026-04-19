#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading;

namespace StrongTypes;

/// <summary>
/// Extensions over <see cref="Enum"/> types. Exposes factories
/// (<c>Parse</c>, <c>TryParse</c>, <c>Create</c>, <c>TryCreate</c>), cached
/// metadata (<c>AllValues</c>, <c>AllFlagValues</c>, <c>AllFlagsCombined</c>),
/// and a per-value <c>GetFlags</c> decomposition.
/// </summary>
public static class EnumExtensions
{
    extension<TEnum>(TEnum) where TEnum : struct, Enum
    {
        /// <summary>Thin wrapper over <see cref="Enum.Parse{TEnum}(string)"/>. Throws on failure.</summary>
        public static TEnum Parse(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Thin wrapper over <see cref="Enum.Parse{TEnum}(string, bool)"/>. Throws on failure.</summary>
        public static TEnum Parse(string value, bool ignoreCase) => Enum.Parse<TEnum>(value, ignoreCase);

        /// <summary>Thin wrapper over <see cref="Enum.TryParse{TEnum}(string, out TEnum)"/>. Returns <c>null</c> on failure.</summary>
        public static TEnum? TryParse(string? value) =>
            Enum.TryParse<TEnum>(value, out var v) ? v : null;

        /// <summary>Thin wrapper over <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>. Returns <c>null</c> on failure.</summary>
        public static TEnum? TryParse(string? value, bool ignoreCase) =>
            Enum.TryParse<TEnum>(value, ignoreCase, out var v) ? v : null;

        /// <summary>Alias for Parse, matching the repo's validated-type factory naming.</summary>
        public static TEnum Create(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Alias for TryParse, matching the repo's validated-type factory naming.</summary>
        public static TEnum? TryCreate(string? value) =>
            Enum.TryParse<TEnum>(value, out var v) ? v : null;

        /// <summary>All declared values of <typeparamref name="TEnum"/>, cached on first type use.</summary>
        public static IReadOnlyList<TEnum> AllValues => EnumMeta<TEnum>.Values;

        /// <summary>
        /// Members of <typeparamref name="TEnum"/> whose underlying bits form
        /// a single power of two. Computed and cached the first time it's
        /// read. Throws if <typeparamref name="TEnum"/> lacks <c>[Flags]</c>.
        /// </summary>
        public static IReadOnlyList<TEnum> AllFlagValues => EnumMeta<TEnum>.FlagValues;

        /// <summary>
        /// Every single-bit flag OR-ed into one value — suitable for
        /// persisting the complete flag set as a single integer. Cached the
        /// first time it's read. Throws if <typeparamref name="TEnum"/>
        /// lacks <c>[Flags]</c>.
        /// </summary>
        public static TEnum AllFlagsCombined => EnumMeta<TEnum>.FlagsCombined;
    }

    extension<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        /// <summary>
        /// Splits the receiver into the individual single-bit flags it
        /// contains, in declaration order. Returns an empty list for zero.
        /// Throws if <typeparamref name="TEnum"/> lacks <c>[Flags]</c>.
        /// </summary>
        public IReadOnlyList<TEnum> GetFlags()
        {
            // Access FlagValues first so non-[Flags] enums throw even when
            // the receiver is zero.
            var flags = EnumMeta<TEnum>.FlagValues;

            var bits = EnumMeta<TEnum>.ToLong(value);
            if (bits == 0)
            {
                return Array.Empty<TEnum>();
            }

            var matched = new List<TEnum>(flags.Count);
            foreach (var flag in flags)
            {
                var flagBits = EnumMeta<TEnum>.ToLong(flag);
                if ((bits & flagBits) == flagBits)
                {
                    matched.Add(flag);
                }
            }
            return matched;
        }
    }
}

internal static class EnumMeta<TEnum> where TEnum : struct, Enum
{
    // Eager one-shot fields: paid once on first touch of this closed generic,
    // no null-check on subsequent reads.
    public static readonly IReadOnlyList<TEnum> Values = Enum.GetValues<TEnum>();
    public static readonly Func<TEnum, long> ToLong = CompileToLong();
    public static readonly Func<long, TEnum> FromLong = CompileFromLong();

    private static readonly bool HasFlagsAttribute =
        typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    // Per-property lazy caches so an enum that never asks for flag data
    // never pays for a flag scan, and vice versa.
    //
    // LazyInitializer.EnsureInitialized gives us double-checked locking:
    // the fast path is a plain bool read + value read; on the slow path
    // it Monitor.Enters the (lazily-allocated) lock object, re-checks the
    // flag, runs the factory once, publishes the result, and releases.
    // Gives us exactly-once compute without the size/atomicity concerns
    // of a bare Nullable<TEnum> backing.

    private static IReadOnlyList<TEnum> _flagValues = null!;
    private static bool _flagValuesReady;
    private static object? _flagValuesLock;
    public static IReadOnlyList<TEnum> FlagValues =>
        LazyInitializer.EnsureInitialized(
            ref _flagValues, ref _flagValuesReady, ref _flagValuesLock, ScanForFlagValues);

    private static TEnum _flagsCombined;
    private static bool _flagsCombinedReady;
    private static object? _flagsCombinedLock;
    public static TEnum FlagsCombined =>
        LazyInitializer.EnsureInitialized(
            ref _flagsCombined, ref _flagsCombinedReady, ref _flagsCombinedLock, OrAllFlagValues);

    private static void RequireFlagsAttribute()
    {
        if (!HasFlagsAttribute)
        {
            throw new InvalidOperationException(
                $"{typeof(TEnum).FullName} is not a [Flags] enum; flag-related APIs are unavailable.");
        }
    }

    // Validation is done inside the factory (not at each getter call) so a
    // well-formed flag enum pays only the LazyInitializer fast path on
    // subsequent reads. Factory throws propagate through LazyInitializer
    // without caching, so non-flag enums still throw on every access.
    //
    // BitOperations.IsPow2 treats negatives as non-flags, which is what we
    // want: a power of two is by definition positive, so a sign-extended
    // high bit on a signed underlying type is excluded.
    private static IReadOnlyList<TEnum> ScanForFlagValues()
    {
        RequireFlagsAttribute();
        return Values.Where(v => BitOperations.IsPow2(ToLong(v))).ToArray();
    }

    private static TEnum OrAllFlagValues()
    {
        long bits = 0;
        foreach (var flag in FlagValues)
        {
            bits |= ToLong(flag);
        }
        return FromLong(bits);
    }

    private static Func<TEnum, long> CompileToLong()
    {
        // (long)(TUnderlying)value — unchecked widening, sign-extending
        // through the underlying integral type.
        var param = Expression.Parameter(typeof(TEnum), "v");
        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var body = Expression.Convert(Expression.Convert(param, underlying), typeof(long));
        return Expression.Lambda<Func<TEnum, long>>(body, param).Compile();
    }

    private static Func<long, TEnum> CompileFromLong()
    {
        // (TEnum)(TUnderlying)bits — unchecked narrowing; truncates when
        // the underlying type is smaller than long.
        var param = Expression.Parameter(typeof(long), "bits");
        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var body = Expression.Convert(Expression.Convert(param, underlying), typeof(TEnum));
        return Expression.Lambda<Func<long, TEnum>>(body, param).Compile();
    }
}
