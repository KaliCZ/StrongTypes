#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace StrongTypes;

/// <summary>
/// Extensions over <see cref="Enum"/> types. Exposes cached metadata
/// (<see cref="AllValues{TEnum}"/>, <see cref="AllFlagValues{TEnum}"/>,
/// <see cref="AllFlagsCombined{TEnum}"/>) plus <see cref="GetFlags{TEnum}"/>
/// for decomposing a flag value into its constituent single-bit flags.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// All declared values of <typeparamref name="TEnum"/>, cached after
    /// the first call.
    /// </summary>
    public static IReadOnlyList<TEnum> AllValues<TEnum>() where TEnum : struct, Enum =>
        EnumCache<TEnum>.AllValues;

    /// <summary>
    /// The subset of <see cref="AllValues{TEnum}"/> whose underlying integral
    /// representation is a single bit (i.e. a power of two). Cached after the
    /// first call.
    /// </summary>
    public static IReadOnlyList<TEnum> AllFlagValues<TEnum>() where TEnum : struct, Enum =>
        EnumCache<TEnum>.AllFlagValues;

    /// <summary>
    /// All single-bit flags of <typeparamref name="TEnum"/> combined into a
    /// single value with bitwise OR — suitable for persisting the full flag
    /// set as one integer. Cached after the first call.
    /// </summary>
    public static TEnum AllFlagsCombined<TEnum>() where TEnum : struct, Enum =>
        EnumCache<TEnum>.AllFlagsCombined;

    /// <summary>
    /// Decomposes <paramref name="value"/> into the individual single-bit
    /// flags it contains, in declaration order. A zero value yields an empty
    /// list. Set bits outside the declared flags of <typeparamref name="TEnum"/>
    /// are ignored.
    /// </summary>
    public static IReadOnlyList<TEnum> GetFlags<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var valueBits = EnumCache<TEnum>.ToUInt64(value);
        if (valueBits == 0)
        {
            return Array.Empty<TEnum>();
        }

        var flags = EnumCache<TEnum>.AllFlagValues;
        var flagBits = EnumCache<TEnum>.AllFlagValueBits;

        var result = new List<TEnum>(flags.Count);
        for (var i = 0; i < flags.Count; i++)
        {
            if ((valueBits & flagBits[i]) == flagBits[i])
            {
                result.Add(flags[i]);
            }
        }
        return result;
    }
}

internal static class EnumCache<TEnum> where TEnum : struct, Enum
{
    public static readonly IReadOnlyList<TEnum> AllValues;
    public static readonly IReadOnlyList<TEnum> AllFlagValues;
    public static readonly IReadOnlyList<ulong> AllFlagValueBits;
    public static readonly TEnum AllFlagsCombined;

    // UInt64-backed enums can hold values above long.MaxValue, which
    // Convert.ToInt64 refuses. Every other underlying type (including
    // signed ones with negative values) round-trips cleanly via
    // Convert.ToInt64 + an unchecked reinterpret to ulong.
    private static readonly bool IsUInt64Underlying =
        Enum.GetUnderlyingType(typeof(TEnum)) == typeof(ulong);

    static EnumCache()
    {
        AllValues = Enum.GetValues<TEnum>();

        var flagValues = new List<TEnum>();
        var flagBits = new List<ulong>();
        ulong combined = 0;
        foreach (var v in AllValues)
        {
            var bits = ToUInt64(v);
            if (bits != 0 && BitOperations.IsPow2(bits))
            {
                flagValues.Add(v);
                flagBits.Add(bits);
                combined |= bits;
            }
        }

        AllFlagValues = flagValues;
        AllFlagValueBits = flagBits;
        AllFlagsCombined = (TEnum)Enum.ToObject(typeof(TEnum), combined);
    }

    public static ulong ToUInt64(TEnum value) =>
        IsUInt64Underlying
            ? (ulong)(object)value
            : unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
