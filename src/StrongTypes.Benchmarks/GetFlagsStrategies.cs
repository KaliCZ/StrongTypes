using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace StrongTypes.Benchmarks;

/// <summary>
/// Standalone reimplementations of GetFlags using four different caching
/// strategies. Each takes the pre-built cache as input so the benchmark
/// measures only the hot-path cost, not cache construction.
/// </summary>
internal static class GetFlagsStrategies
{
    public static Func<TEnum, long> CompileToLong<TEnum>() where TEnum : struct, Enum
    {
        var param = Expression.Parameter(typeof(TEnum), "v");
        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var body = Expression.Convert(Expression.Convert(param, underlying), typeof(long));
        return Expression.Lambda<Func<TEnum, long>>(body, param).Compile();
    }

    public static TEnum[] BuildFlagValues<TEnum>(Func<TEnum, long> toLong) where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().Where(v => System.Numerics.BitOperations.IsPow2(toLong(v))).ToArray();

    public static long[] BuildFlagBits<TEnum>(TEnum[] flags, Func<TEnum, long> toLong) where TEnum : struct, Enum
    {
        var bits = new long[flags.Length];
        for (var i = 0; i < flags.Length; i++) bits[i] = toLong(flags[i]);
        return bits;
    }

    public static (TEnum Flag, long Bits)[] BuildFlagPairs<TEnum>(TEnum[] flags, Func<TEnum, long> toLong) where TEnum : struct, Enum
    {
        var pairs = new (TEnum, long)[flags.Length];
        for (var i = 0; i < flags.Length; i++) pairs[i] = (flags[i], toLong(flags[i]));
        return pairs;
    }

    public static FrozenDictionary<TEnum, long> BuildFrozenDict<TEnum>(TEnum[] flags, Func<TEnum, long> toLong) where TEnum : struct, Enum
        => flags.ToDictionary(f => f, toLong).ToFrozenDictionary();

    public static IReadOnlyList<TEnum> Current<TEnum>(TEnum source, TEnum[] flags, Func<TEnum, long> toLong)
        where TEnum : struct, Enum
    {
        var bits = toLong(source);
        if (bits == 0) return Array.Empty<TEnum>();
        var matched = new List<TEnum>(flags.Length);
        foreach (var flag in flags)
        {
            var fb = toLong(flag);
            if ((bits & fb) == fb) matched.Add(flag);
        }
        return matched;
    }

    public static IReadOnlyList<TEnum> ParallelArray<TEnum>(TEnum source, TEnum[] flags, long[] flagBits, Func<TEnum, long> toLong)
        where TEnum : struct, Enum
    {
        var bits = toLong(source);
        if (bits == 0) return Array.Empty<TEnum>();
        var matched = new List<TEnum>(flags.Length);
        for (var i = 0; i < flags.Length; i++)
        {
            var fb = flagBits[i];
            if ((bits & fb) == fb) matched.Add(flags[i]);
        }
        return matched;
    }

    public static IReadOnlyList<TEnum> PairArray<TEnum>(TEnum source, (TEnum Flag, long Bits)[] pairs, Func<TEnum, long> toLong)
        where TEnum : struct, Enum
    {
        var bits = toLong(source);
        if (bits == 0) return Array.Empty<TEnum>();
        var matched = new List<TEnum>(pairs.Length);
        foreach (var pair in pairs)
        {
            if ((bits & pair.Bits) == pair.Bits) matched.Add(pair.Flag);
        }
        return matched;
    }

    public static IReadOnlyList<TEnum> FrozenDict<TEnum>(TEnum source, FrozenDictionary<TEnum, long> lookup, Func<TEnum, long> toLong)
        where TEnum : struct, Enum
    {
        var bits = toLong(source);
        if (bits == 0) return Array.Empty<TEnum>();
        var matched = new List<TEnum>(lookup.Count);
        foreach (var kv in lookup)
        {
            if ((bits & kv.Value) == kv.Value) matched.Add(kv.Key);
        }
        return matched;
    }
}
