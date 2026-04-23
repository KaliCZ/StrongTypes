using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace StrongTypes.Benchmarks;

[MemoryDiagnoser]
public class GetFlags5Benchmarks
{
    private readonly Func<Flags5, long> _toLong = GetFlagsStrategies.CompileToLong<Flags5>();
    private Flags5[] _flags = null!;
    private long[] _flagBits = null!;
    private (Flags5 Flag, long Bits)[] _flagPairs = null!;
    private FrozenDictionary<Flags5, long> _frozen = null!;
    private Flags5 _value;

    [GlobalSetup]
    public void Setup()
    {
        _flags = GetFlagsStrategies.BuildFlagValues(_toLong);
        _flagBits = GetFlagsStrategies.BuildFlagBits(_flags, _toLong);
        _flagPairs = GetFlagsStrategies.BuildFlagPairs(_flags, _toLong);
        _frozen = GetFlagsStrategies.BuildFrozenDict(_flags, _toLong);
        // Typical case: ~half the flags set.
        _value = Flags5.F0 | Flags5.F2 | Flags5.F4;
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<Flags5> Current() => GetFlagsStrategies.Current(_value, _flags, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags5> ParallelArray() => GetFlagsStrategies.ParallelArray(_value, _flags, _flagBits, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags5> PairArray() => GetFlagsStrategies.PairArray(_value, _flagPairs, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags5> FrozenDict() => GetFlagsStrategies.FrozenDict(_value, _frozen, _toLong);
}

[MemoryDiagnoser]
public class GetFlags15Benchmarks
{
    private readonly Func<Flags15, long> _toLong = GetFlagsStrategies.CompileToLong<Flags15>();
    private Flags15[] _flags = null!;
    private long[] _flagBits = null!;
    private (Flags15 Flag, long Bits)[] _flagPairs = null!;
    private FrozenDictionary<Flags15, long> _frozen = null!;
    private Flags15 _value;

    [GlobalSetup]
    public void Setup()
    {
        _flags = GetFlagsStrategies.BuildFlagValues(_toLong);
        _flagBits = GetFlagsStrategies.BuildFlagBits(_flags, _toLong);
        _flagPairs = GetFlagsStrategies.BuildFlagPairs(_flags, _toLong);
        _frozen = GetFlagsStrategies.BuildFrozenDict(_flags, _toLong);
        _value = Flags15.F0 | Flags15.F2 | Flags15.F4 | Flags15.F6 | Flags15.F8 | Flags15.F10 | Flags15.F12 | Flags15.F14;
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<Flags15> Current() => GetFlagsStrategies.Current(_value, _flags, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags15> ParallelArray() => GetFlagsStrategies.ParallelArray(_value, _flags, _flagBits, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags15> PairArray() => GetFlagsStrategies.PairArray(_value, _flagPairs, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags15> FrozenDict() => GetFlagsStrategies.FrozenDict(_value, _frozen, _toLong);
}

[MemoryDiagnoser]
public class GetFlags30Benchmarks
{
    private readonly Func<Flags30, long> _toLong = GetFlagsStrategies.CompileToLong<Flags30>();
    private Flags30[] _flags = null!;
    private long[] _flagBits = null!;
    private (Flags30 Flag, long Bits)[] _flagPairs = null!;
    private FrozenDictionary<Flags30, long> _frozen = null!;
    private Flags30 _value;

    [GlobalSetup]
    public void Setup()
    {
        _flags = GetFlagsStrategies.BuildFlagValues(_toLong);
        _flagBits = GetFlagsStrategies.BuildFlagBits(_flags, _toLong);
        _flagPairs = GetFlagsStrategies.BuildFlagPairs(_flags, _toLong);
        _frozen = GetFlagsStrategies.BuildFrozenDict(_flags, _toLong);
        _value = Flags30.F0 | Flags30.F3 | Flags30.F6 | Flags30.F9 | Flags30.F12 | Flags30.F15 | Flags30.F18 | Flags30.F21 | Flags30.F24 | Flags30.F27;
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<Flags30> Current() => GetFlagsStrategies.Current(_value, _flags, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags30> ParallelArray() => GetFlagsStrategies.ParallelArray(_value, _flags, _flagBits, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags30> PairArray() => GetFlagsStrategies.PairArray(_value, _flagPairs, _toLong);

    [Benchmark]
    public IReadOnlyList<Flags30> FrozenDict() => GetFlagsStrategies.FrozenDict(_value, _frozen, _toLong);
}
