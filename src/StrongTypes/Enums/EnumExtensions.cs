using System;
using System.Diagnostics.Contracts;
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
    extension<TEnum>(TEnum source) where TEnum : struct, Enum
    {
        /// <summary>Thin wrapper over <see cref="Enum.Parse{TEnum}(string)"/>. Throws on failure.</summary>
        [Pure]
        public static TEnum Parse(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Thin wrapper over <see cref="Enum.Parse{TEnum}(string, bool)"/>. Throws on failure.</summary>
        [Pure]
        public static TEnum Parse(string value, bool ignoreCase) => Enum.Parse<TEnum>(value, ignoreCase);

        /// <summary>Thin wrapper over <see cref="Enum.TryParse{TEnum}(string, out TEnum)"/>. Returns <c>null</c> on failure.</summary>
        [Pure]
        public static TEnum? TryParse(string? value) => Enum.TryParse<TEnum>(value, out var v) ? v : null;

        /// <summary>Thin wrapper over <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>. Returns <c>null</c> on failure.</summary>
        [Pure]
        public static TEnum? TryParse(string? value, bool ignoreCase) => Enum.TryParse<TEnum>(value, ignoreCase, out var v) ? v : null;

        /// <summary>Alias for Parse, matching the repo's validated-type factory naming.</summary>
        [Pure]
        public static TEnum Create(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Alias for TryParse, matching the repo's validated-type factory naming.</summary>
        [Pure]
        public static TEnum? TryCreate(string? value) => Enum.TryParse<TEnum>(value, out var v) ? v : null;

        /// <summary>All declared values of <typeparamref name="TEnum"/>, cached on first type use.</summary>
        [Pure]
        public static TEnum[] AllValues => EnumMeta<TEnum>.Values;

        /// <summary>
        /// Members of <typeparamref name="TEnum"/> whose underlying bits form
        /// a single power of two. Computed and cached the first time it's
        /// read. Throws if <typeparamref name="TEnum"/> lacks <c>[Flags]</c>.
        /// </summary>
        [Pure]
        public static TEnum[] AllFlagValues => FlagEnumMeta<TEnum>.FlagValues;

        /// <summary>
        /// Every single-bit flag OR-ed into one value — suitable for
        /// persisting the complete flag set as a single integer. Cached the
        /// first time it's read. Throws if <typeparamref name="TEnum"/>
        /// lacks <c>[Flags]</c>.
        /// </summary>
        [Pure]
        public static TEnum AllFlagsCombined => FlagEnumMeta<TEnum>.FlagsCombined;

        /// <summary>
        /// Splits the receiver into the individual single-bit flags it
        /// contains, in declaration order. Returns an empty list for zero.
        /// Throws if <typeparamref name="TEnum"/> lacks <c>[Flags]</c>.
        /// </summary>
        [Pure]
        public IReadOnlyList<TEnum> GetFlags()
        {
            // Access FlagValues first so non-[Flags] enums throw even when
            // the receiver is zero.
            var flags = FlagEnumMeta<TEnum>.FlagValues;

            var bits = FlagEnumMeta<TEnum>.ToLong(source);
            if (bits == 0)
            {
                return Array.Empty<TEnum>();
            }

            var matched = new List<TEnum>(flags.Length);
            foreach (var flag in flags)
            {
                var flagBits = FlagEnumMeta<TEnum>.ToLong(flag);
                if ((bits & flagBits) == flagBits)
                {
                    matched.Add(flag);
                }
            }
            return matched;
        }
    }
}

// Split in two so non-flag enums never pay for the flag-related state:
// reflecting for [Flags], compiling the ToLong/FromLong conversions, and
// allocating the lazy caches. Touching AllValues on a plain enum only
// cctors EnumMeta; FlagEnumMeta's cctor fires only when flag APIs are used.
internal static class EnumMeta<TEnum> where TEnum : struct, Enum
{
    public static readonly TEnum[] Values = Enum.GetValues<TEnum>();
}

internal static class FlagEnumMeta<TEnum> where TEnum : struct, Enum
{
    public static readonly Func<TEnum, long> ToLong = CompileToLong();
    public static readonly Func<long, TEnum> FromLong = CompileFromLong();

    private static readonly bool HasFlagsAttribute = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    // FlagValues is a reference: a plain ??= is enough. A race can run
    // ScanForFlagValues more than once, but the scan is deterministic so
    // last-write-wins is benign, and .NET guarantees the array's writes
    // are visible before its reference is published.
    private static TEnum[]? _flagValues;
    [Pure]
    public static TEnum[] FlagValues => _flagValues ??= ScanForFlagValues();

    // FlagsCombined is a TEnum (up to 8 bytes; not atomic on 32-bit) and
    // default(TEnum) == 0 is a valid computed result, so we can't use
    // the value itself as a freshness marker. A separate bool gives us
    // that marker; LazyInitializer handles the DCL and release barrier.
    private static TEnum _flagsCombined;
    private static bool _flagsCombinedReady;
    private static object? _flagsCombinedLock;
    [Pure]
    public static TEnum FlagsCombined => LazyInitializer.EnsureInitialized(
        ref _flagsCombined, ref _flagsCombinedReady, ref _flagsCombinedLock, OrAllFlagValues
    );

    // Validation is done inside the factory (not at each getter call) so a
    // well-formed flag enum pays only the LazyInitializer fast path on
    // subsequent reads. Factory throws propagate through LazyInitializer
    // without caching, so non-flag enums still throw on every access.
    //
    // BitOperations.IsPow2 treats negatives as non-flags, which is what we
    // want: a power of two is by definition positive, so a sign-extended
    // high bit on a signed underlying type is excluded.
    private static TEnum[] ScanForFlagValues()
    {
        if (!HasFlagsAttribute)
            throw new InvalidOperationException($"{typeof(TEnum).FullName} is not a [Flags] enum; flag-related APIs are unavailable.");
        return EnumMeta<TEnum>.Values.Where(v => BitOperations.IsPow2(ToLong(v))).ToArray();
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
