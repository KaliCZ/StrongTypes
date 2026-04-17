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

        /// <summary>Alias for Parse, matching the repo's validated-type factory naming.</summary>
        public static TEnum Create(string value) => Enum.Parse<TEnum>(value);

        /// <summary>Alias for TryParse, matching the repo's validated-type factory naming.</summary>
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

            var flags = EnumCache<TEnum>.AllFlagValuesUnchecked;
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
    // Each cache is its own ??= field so enums that never touch flag APIs
    // never pay for a flag scan, and vice versa. ??= is racey under
    // contention — two threads may both run the compute — but every producer
    // yields the same result and reference-sized writes are atomic, so any
    // observer sees either null or a fully-constructed value.
    //
    // The combined Values/Bits/Combined data lives in a single FlagMeta
    // record class (a reference) so that one atomic pointer write publishes
    // every flag-related field at once. A bare Nullable<TEnum> backing
    // wouldn't be safe here: Nullable<T> is an inline struct and for
    // long-backed enums its 16-byte layout isn't written atomically, so a
    // reader could see HasValue=true with a torn (default) Value.

    // FlagsAttribute lookup only runs once per closed generic type.
    private static readonly bool IsFlagsEnum =
        typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    private static IReadOnlyList<TEnum>? _allValues;
    public static IReadOnlyList<TEnum> AllValues => _allValues ??= Enum.GetValues<TEnum>();

    private static Func<TEnum, long>? _toLong;
    public static long ToLong(TEnum value) => (_toLong ??= CreateToLong()).Invoke(value);

    private static Func<long, TEnum>? _fromLong;
    private static TEnum FromLong(long bits) => (_fromLong ??= CreateFromLong()).Invoke(bits);

    // Published-flag pattern for the flag metadata: the writer fills in the
    // three value fields and then Volatile.Writes _flagMetaReady=true. Readers
    // Volatile.Read the flag first and only touch the fields if it's true,
    // so they can never observe a half-initialized state. Under contention
    // multiple threads may compute, but the compute is deterministic so
    // identical bits land in each field regardless of ordering.
    private static IReadOnlyList<TEnum> _flagValues = Array.Empty<TEnum>();
    private static long[] _flagBits = Array.Empty<long>();
    private static TEnum _flagsCombined;
    private static bool _flagMetaReady;

    private static void EnsureFlagMeta()
    {
        if (Volatile.Read(ref _flagMetaReady))
        {
            return;
        }

        var values = AllValues;
        var flagValues = new List<TEnum>(values.Count);
        var flagBits = new List<long>(values.Count);
        long combined = 0;
        foreach (var v in values)
        {
            var bits = ToLong(v);
            // A power of two must be positive. Negative values (including a
            // sign-extended high bit on a signed underlying type) are excluded.
            if (bits > 0 && BitOperations.IsPow2(bits))
            {
                flagValues.Add(v);
                flagBits.Add(bits);
                combined |= bits;
            }
        }

        _flagValues = flagValues;
        _flagBits = flagBits.ToArray();
        _flagsCombined = FromLong(combined);
        Volatile.Write(ref _flagMetaReady, true);
    }

    public static IReadOnlyList<TEnum> AllFlagValues
    {
        get { EnsureFlagsEnum(); EnsureFlagMeta(); return _flagValues; }
    }

    public static IReadOnlyList<TEnum> AllFlagValuesUnchecked
    {
        get { EnsureFlagMeta(); return _flagValues; }
    }

    public static long[] AllFlagValueBits
    {
        get { EnsureFlagMeta(); return _flagBits; }
    }

    public static TEnum AllFlagsCombined
    {
        get { EnsureFlagsEnum(); EnsureFlagMeta(); return _flagsCombined; }
    }

    public static void EnsureFlagsEnum()
    {
        if (!IsFlagsEnum)
        {
            throw new InvalidOperationException(
                $"{typeof(TEnum).FullName} is not a [Flags] enum; flag-related APIs are unavailable.");
        }
    }

    private static Func<TEnum, long> CreateToLong()
    {
        // Emit: (long)(TUnderlying)value — unchecked widening via the
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
