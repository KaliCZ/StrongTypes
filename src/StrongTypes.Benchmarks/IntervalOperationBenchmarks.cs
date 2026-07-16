using System;
using BenchmarkDotNet.Attributes;

namespace StrongTypes.Benchmarks;

// The bridging Contains(DateOnly) builds a temporary day window, so it is the one case that could allocate.
[MemoryDiagnoser]
public class IntervalOperationBenchmarks
{
    private readonly Interval<int> _a = Interval.Create(0, 100);
    private readonly Interval<int> _b = Interval.Create(50, 150);
    private readonly FiniteInterval<DateTime> _window =
        FiniteInterval.Create(new DateTime(2020, 1, 1), new DateTime(2020, 1, 10));
    private readonly DateOnly _day = new(2020, 1, 5);

    [Benchmark]
    public bool Overlaps() => _a.Overlaps(_b);

    [Benchmark]
    public Interval<int>? GetOverlap() => _a.GetOverlap(_b);

    [Benchmark]
    public bool Contains() => _a.Contains(42);

    [Benchmark]
    public bool ContainsCalendarDay() => _window.Contains(_day);
}
