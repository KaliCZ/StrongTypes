#nullable enable

using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultAggregateTests
{
    // ── Arity 2 — tuple ────────────────────────────────────────────────

    [Property]
    public void Aggregate2_AllSuccess_ReturnsTuple(int a, string b)
    {
        Result<int, string> r1 = a;
        Result<int, string> r2 = 42;

        var r = Result.Aggregate(r1, r2);
        Assert.True(r.IsSuccess);
        Assert.Equal((a, 42), r.Success);
    }

    [Property]
    public void Aggregate2_FirstError_ReturnsSingleError(string e1, int v2)
    {
        Result<int, string> r1 = e1;
        Result<int, string> r2 = v2;

        var r = Result.Aggregate(r1, r2);
        Assert.True(r.IsError);
        Assert.Equal(new[] { e1 }, r.Error);
    }

    [Property]
    public void Aggregate2_BothError_CollectsBothInOrder(string e1, string e2)
    {
        Result<int, string> r1 = e1;
        Result<int, string> r2 = e2;

        var r = Result.Aggregate(r1, r2);
        Assert.Equal(new[] { e1, e2 }, r.Error);
    }

    // ── Arity 2 — combiner ─────────────────────────────────────────────

    [Property]
    public void Aggregate2_Combiner_AllSuccess_InvokesCombine(int a, int b)
    {
        Result<int, string> r1 = a;
        Result<int, string> r2 = b;

        var r = Result.Aggregate(r1, r2, (x, y) => x + y);
        Assert.Equal(a + b, r.Success);
    }

    [Property]
    public void Aggregate2_Combiner_Error_DoesNotInvokeCombine(string e1)
    {
        Result<int, string> r1 = e1;
        Result<int, string> r2 = 5;
        var invoked = false;

        var r = Result.Aggregate(r1, r2, (x, y) => { invoked = true; return x + y; });
        Assert.False(invoked);
        Assert.Equal(new[] { e1 }, r.Error);
    }

    // ── Arity 8 — smoke tests covering the TRest-nested tuple path ─────

    [Fact]
    public void Aggregate8_AllSuccess_ReturnsFlatTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = 8;

        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), r.Success);
    }

    [Fact]
    public void Aggregate8_MixedErrors_CollectsOnlyErrorsInOrder()
    {
        Result<int, string> r1 = 1;
        Result<int, string> r2 = "e2";
        Result<int, string> r3 = 3;
        Result<int, string> r4 = "e4";
        Result<int, string> r5 = 5;
        Result<int, string> r6 = 6;
        Result<int, string> r7 = "e7";
        Result<int, string> r8 = 8;

        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8);
        Assert.Equal(new[] { "e2", "e4", "e7" }, r.Error);
    }

    [Fact]
    public void Aggregate8_Combiner_AllSuccess_InvokesCombine()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = 8;

        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8,
            (a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h);
        Assert.Equal(36, r.Success);
    }

    // ── IEnumerable form ───────────────────────────────────────────────

    [Property]
    public void AggregateEnumerable_AllSuccess_ReturnsValuesInOrder(int[] values)
    {
        var results = values.Select(v => (Result<int, string>)v);

        var r = Result.Aggregate(results);
        Assert.True(r.IsSuccess);
        Assert.Equal(values, r.Success);
    }

    [Property]
    public void AggregateEnumerable_AllErrors_ReturnsErrorsInOrder(string[] errors)
    {
        // FsCheck can generate null entries inside the array; filter since our
        // TError : notnull constraint rejects those.
        var clean = errors.Where(e => e is not null).ToArray();
        var results = clean.Select(e => (Result<int, string>)e);

        var r = Result.Aggregate(results);
        if (clean.Length == 0)
        {
            Assert.True(r.IsSuccess);
            Assert.Empty(r.Success!);
        }
        else
        {
            Assert.True(r.IsError);
            Assert.Equal(clean, r.Error);
        }
    }

    [Fact]
    public void AggregateEnumerable_Mixed_ReturnsOnlyErrors()
    {
        var results = new List<Result<int, string>>
        {
            1, "e1", 2, 3, "e2", 4,
        };

        var r = Result.Aggregate(results);
        Assert.Equal(new[] { "e1", "e2" }, r.Error);
    }

    [Fact]
    public void AggregateEnumerable_Empty_ReturnsEmptySuccessList()
    {
        var results = new List<Result<int, string>>();

        var r = Result.Aggregate(results);
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Success!);
    }

    [Fact]
    public void AggregateEnumerable_DrainsSequenceEvenOnError()
    {
        var drawCount = 0;
        IEnumerable<Result<int, string>> Source()
        {
            drawCount++; yield return 1;
            drawCount++; yield return "e1";
            drawCount++; yield return 2;
            drawCount++; yield return "e2";
        }

        var r = Result.Aggregate(Source());
        Assert.Equal(4, drawCount);
        Assert.Equal(new[] { "e1", "e2" }, r.Error);
    }

    // ── Round-trip: the combiner is just a tuple-form + Map ────────────

    [Property]
    public void Aggregate_CombinerEquivalentToTupleThenMap(int a, int b)
    {
        Result<int, string> r1 = a;
        Result<int, string> r2 = b;

        var combiner = Result.Aggregate(r1, r2, (x, y) => x * y);
        var viaTuple = Result.Aggregate(r1, r2).Map(t => t.Item1 * t.Item2);

        Assert.Equal(combiner, viaTuple);
    }
}
