#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;

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
        public static IReadOnlyList<TEnum> AllFlagValues
        {
            get
            {
                EnumMeta<TEnum>.RequireFlagsAttribute();
                return EnumMeta<TEnum>.FlagValues;
            }
        }

        /// <summary>
        /// Every single-bit flag OR-ed into one value — suitable for
        /// persisting the complete flag set as a single integer. Cached the
        /// first time it's read. Throws if <typeparamref name="TEnum"/>
        /// lacks <c>[Flags]</c>.
        /// </summary>
        public static TEnum AllFlagsCombined
        {
            get
            {
                EnumMeta<TEnum>.RequireFlagsAttribute();
                return EnumMeta<TEnum>.FlagsCombined;
            }
        }
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
            EnumMeta<TEnum>.RequireFlagsAttribute();

            var bits = EnumMeta<TEnum>.ToLong(value);
            if (bits == 0)
            {
                return Array.Empty<TEnum>();
            }

            var flags = EnumMeta<TEnum>.FlagValues;
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
    // never pays for a flag scan, and vice versa. ??= is racey under
    // contention but the compute is deterministic, so all producers store
    // identical bits.
    //
    // FlagValues' backing is a reference, so the write is atomic. FlagsCombined
    // uses Nullable<TEnum> which is an inline struct: for byte/short/int-backed
    // enums it fits in 8 bytes and writes atomically on 64-bit runtimes; for
    // long/ulong-backed enums the 16-byte layout could tear under
    // first-access contention. Acceptable for the narrow edge case.

    private static IReadOnlyList<TEnum>? _flagValues;
    public static IReadOnlyList<TEnum> FlagValues => _flagValues ??= ScanForFlagValues();

    private static TEnum? _flagsCombined;
    public static TEnum FlagsCombined => _flagsCombined ??= OrAllFlagValues();

    public static void RequireFlagsAttribute()
    {
        if (!HasFlagsAttribute)
        {
            throw new InvalidOperationException(
                $"{typeof(TEnum).FullName} is not a [Flags] enum; flag-related APIs are unavailable.");
        }
    }

    private static IReadOnlyList<TEnum> ScanForFlagValues()
    {
        var flags = new List<TEnum>();
        foreach (var v in Values)
        {
            // BitOperations.IsPow2 treats negatives as non-flags, which is
            // what we want: a power of two is by definition positive.
            if (BitOperations.IsPow2(ToLong(v)))
            {
                flags.Add(v);
            }
        }
        return flags;
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
