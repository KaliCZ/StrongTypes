#nullable enable

using System;
using System.Collections.Generic;
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

        /// <summary>Alias for <see cref="Parse(string)"/>, matching the repo's validated-type factory naming.</summary>
        public static TEnum Create(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Alias for <see cref="TryParse(string)"/>, matching the repo's validated-type factory naming.</summary>
        public static TEnum? TryCreate(string? value) =>
            Enum.TryParse<TEnum>(value, out var v) ? v : null;

        /// <summary>All declared values of <typeparamref name="TEnum"/>, cached after the first access.</summary>
        public static IReadOnlyList<TEnum> AllValues => EnumCache<TEnum>.AllValues;

        /// <summary>
        /// The subset of <c>AllValues</c> whose underlying integral
        /// representation is a single bit (a power of two). Cached after the
        /// first access. Throws if <typeparamref name="TEnum"/> is not a
        /// <c>[Flags]</c> enum.
        /// </summary>
        public static IReadOnlyList<TEnum> AllFlagValues => EnumCache<TEnum>.AllFlagValues;

        /// <summary>
        /// All single-bit flags of <typeparamref name="TEnum"/> combined into
        /// a single value with bitwise OR — suitable for persisting the full
        /// flag set as one integer. Cached after the first access. Throws if
        /// <typeparamref name="TEnum"/> is not a <c>[Flags]</c> enum.
        /// </summary>
        public static TEnum AllFlagsCombined => EnumCache<TEnum>.AllFlagsCombined;
    }

    extension<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        /// <summary>
        /// Decomposes the receiver into the individual single-bit flags it
        /// contains, in declaration order. A zero value yields an empty list.
        /// Throws if <typeparamref name="TEnum"/> is not a <c>[Flags]</c> enum.
        /// </summary>
        public IReadOnlyList<TEnum> GetFlags()
        {
            EnumCache<TEnum>.EnsureFlagsEnum();
            var bits = EnumCache<TEnum>.ToLong(value);
            if (bits == 0)
            {
                return Array.Empty<TEnum>();
            }

            var flags = EnumCache<TEnum>.AllFlagValues;
            var flagBits = EnumCache<TEnum>.AllFlagValueBits;

            var result = new List<TEnum>(flags.Count);
            for (var i = 0; i < flags.Count; i++)
            {
                if ((bits & flagBits[i]) == flagBits[i])
                {
                    result.Add(flags[i]);
                }
            }
            return result;
        }
    }
}

internal static class EnumCache<TEnum> where TEnum : struct, Enum
{
    // Each cache is independently lazy so that enums that never hit the
    // flag-related APIs don't pay for a flag scan, and vice versa.
    // PublicationOnly trades occasional duplicate work under contention
    // for cheaper access and no lock allocation.
    private static readonly Lazy<IReadOnlyList<TEnum>> _allValues =
        new(static () => Enum.GetValues<TEnum>(), LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<Func<TEnum, long>> _toLong =
        new(CreateToLong, LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<Func<long, TEnum>> _fromLong =
        new(CreateFromLong, LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<(IReadOnlyList<TEnum> Values, long[] Bits)> _flagData =
        new(ComputeFlagData, LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<TEnum> _allFlagsCombined =
        new(ComputeAllFlagsCombined, LazyThreadSafetyMode.PublicationOnly);

    public static IReadOnlyList<TEnum> AllValues => _allValues.Value;

    public static IReadOnlyList<TEnum> AllFlagValues
    {
        get
        {
            EnsureFlagsEnum();
            return _flagData.Value.Values;
        }
    }

    public static long[] AllFlagValueBits => _flagData.Value.Bits;

    public static TEnum AllFlagsCombined
    {
        get
        {
            EnsureFlagsEnum();
            return _allFlagsCombined.Value;
        }
    }

    public static long ToLong(TEnum value) => _toLong.Value(value);

    public static void EnsureFlagsEnum()
    {
        if (!typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            throw new InvalidOperationException(
                $"{typeof(TEnum).FullName} is not a [Flags] enum; flag-related APIs are unavailable.");
        }
    }

    private static (IReadOnlyList<TEnum>, long[]) ComputeFlagData()
    {
        var values = AllValues;
        var toLong = _toLong.Value;
        var flagValues = new List<TEnum>(values.Count);
        var flagBits = new List<long>(values.Count);
        foreach (var v in values)
        {
            var bits = toLong(v);
            // IsPow2 on a signed long treats negatives as non-flags, which is
            // what we want: a power of two is by definition positive.
            if (bits > 0 && BitOperations.IsPow2(bits))
            {
                flagValues.Add(v);
                flagBits.Add(bits);
            }
        }
        return (flagValues, flagBits.ToArray());
    }

    private static TEnum ComputeAllFlagsCombined()
    {
        long combined = 0;
        foreach (var b in _flagData.Value.Bits)
        {
            combined |= b;
        }
        return _fromLong.Value(combined);
    }

    private static Func<TEnum, long> CreateToLong()
    {
        // Emit: (long)(TUnderlying)value  — unchecked widening via the
        // underlying integral type, sign-extending for signed enums.
        var param = Expression.Parameter(typeof(TEnum), "v");
        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var body = Expression.Convert(Expression.Convert(param, underlying), typeof(long));
        return Expression.Lambda<Func<TEnum, long>>(body, param).Compile();
    }

    private static Func<long, TEnum> CreateFromLong()
    {
        // Emit: (TEnum)(TUnderlying)bits — unchecked narrowing; truncates
        // when the underlying type is smaller than long.
        var param = Expression.Parameter(typeof(long), "bits");
        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var body = Expression.Convert(Expression.Convert(param, underlying), typeof(TEnum));
        return Expression.Lambda<Func<long, TEnum>>(body, param).Compile();
    }
}
