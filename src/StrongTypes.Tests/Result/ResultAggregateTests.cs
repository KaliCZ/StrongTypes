#nullable enable

using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultAggregateTests
{
    // ── Arity 2 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate2_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2;
        var r = Result.Aggregate(r1, r2);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2), r.Success);
    }

    [Fact]
    public void Aggregate2_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = "e1", r2 = "e2";
        var r = Result.Aggregate(r1, r2);
        Assert.True(r.IsError);
        Assert.Equal(new[] { "e1", "e2" }, r.Error);
    }

    [Fact]
    public void Aggregate2_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, (a, b) => { calls++; return a + b; });
        Assert.Equal(1, calls);
        Assert.Equal(3, r.Success);
    }

    [Fact]
    public void Aggregate2_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = "e1", r2 = 2;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, (a, b) => { calls++; return a + b; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e1" }, r.Error);
    }

    // ── Arity 3 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate3_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3;
        var r = Result.Aggregate(r1, r2, r3);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3), r.Success);
    }

    [Fact]
    public void Aggregate3_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = "e1", r2 = 2, r3 = "e3";
        var r = Result.Aggregate(r1, r2, r3);
        Assert.Equal(new[] { "e1", "e3" }, r.Error);
    }

    [Fact]
    public void Aggregate3_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, (a, b, c) => { calls++; return a + b + c; });
        Assert.Equal(1, calls);
        Assert.Equal(6, r.Success);
    }

    [Fact]
    public void Aggregate3_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = "e2", r3 = 3;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, (a, b, c) => { calls++; return a + b + c; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e2" }, r.Error);
    }

    // ── Arity 4 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate4_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4;
        var r = Result.Aggregate(r1, r2, r3, r4);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4), r.Success);
    }

    [Fact]
    public void Aggregate4_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = "e1", r2 = 2, r3 = "e3", r4 = "e4";
        var r = Result.Aggregate(r1, r2, r3, r4);
        Assert.Equal(new[] { "e1", "e3", "e4" }, r.Error);
    }

    [Fact]
    public void Aggregate4_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, (a, b, c, d) => { calls++; return a + b + c + d; });
        Assert.Equal(1, calls);
        Assert.Equal(10, r.Success);
    }

    [Fact]
    public void Aggregate4_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = "e3", r4 = 4;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, (a, b, c, d) => { calls++; return a + b + c + d; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e3" }, r.Error);
    }

    // ── Arity 5 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate5_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5;
        var r = Result.Aggregate(r1, r2, r3, r4, r5);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4, 5), r.Success);
    }

    [Fact]
    public void Aggregate5_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = 1, r2 = "e2", r3 = 3, r4 = "e4", r5 = 5;
        var r = Result.Aggregate(r1, r2, r3, r4, r5);
        Assert.Equal(new[] { "e2", "e4" }, r.Error);
    }

    [Fact]
    public void Aggregate5_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5,
            (a, b, c, d, e) => { calls++; return a + b + c + d + e; });
        Assert.Equal(1, calls);
        Assert.Equal(15, r.Success);
    }

    [Fact]
    public void Aggregate5_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = "e5";
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5,
            (a, b, c, d, e) => { calls++; return a + b + c + d + e; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e5" }, r.Error);
    }

    // ── Arity 6 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate6_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4, 5, 6), r.Success);
    }

    [Fact]
    public void Aggregate6_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = "e1", r2 = 2, r3 = 3, r4 = "e4", r5 = 5, r6 = "e6";
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6);
        Assert.Equal(new[] { "e1", "e4", "e6" }, r.Error);
    }

    [Fact]
    public void Aggregate6_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6,
            (a, b, c, d, e, f) => { calls++; return a + b + c + d + e + f; });
        Assert.Equal(1, calls);
        Assert.Equal(21, r.Success);
    }

    [Fact]
    public void Aggregate6_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = "e3", r4 = 4, r5 = 5, r6 = 6;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6,
            (a, b, c, d, e, f) => { calls++; return a + b + c + d + e + f; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e3" }, r.Error);
    }

    // ── Arity 7 ────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate7_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4, 5, 6, 7), r.Success);
    }

    [Fact]
    public void Aggregate7_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = 1, r2 = "e2", r3 = 3, r4 = "e4", r5 = 5, r6 = "e6", r7 = 7;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7);
        Assert.Equal(new[] { "e2", "e4", "e6" }, r.Error);
    }

    [Fact]
    public void Aggregate7_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7,
            (a, b, c, d, e, f, g) => { calls++; return a + b + c + d + e + f + g; });
        Assert.Equal(1, calls);
        Assert.Equal(28, r.Success);
    }

    [Fact]
    public void Aggregate7_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = "e7";
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7,
            (a, b, c, d, e, f, g) => { calls++; return a + b + c + d + e + f + g; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e7" }, r.Error);
    }

    // ── Arity 8 — exercises the TRest-nested ValueTuple path ───────────

    [Fact]
    public void Aggregate8_Tuple_AllSuccess_ReturnsTuple()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = 8;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8);
        Assert.True(r.IsSuccess);
        Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), r.Success);
    }

    [Fact]
    public void Aggregate8_Tuple_Errors_CollectedInInputOrder()
    {
        Result<int, string> r1 = 1, r2 = "e2", r3 = 3, r4 = "e4", r5 = 5, r6 = 6, r7 = "e7", r8 = 8;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8);
        Assert.Equal(new[] { "e2", "e4", "e7" }, r.Error);
    }

    [Fact]
    public void Aggregate8_Combiner_AllSuccess_InvokesCombinerExactlyOnce()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = 8;
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8,
            (a, b, c, d, e, f, g, h) => { calls++; return a + b + c + d + e + f + g + h; });
        Assert.Equal(1, calls);
        Assert.Equal(36, r.Success);
    }

    [Fact]
    public void Aggregate8_Combiner_Error_DoesNotInvokeCombiner()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5, r6 = 6, r7 = 7, r8 = "e8";
        var calls = 0;
        var r = Result.Aggregate(r1, r2, r3, r4, r5, r6, r7, r8,
            (a, b, c, d, e, f, g, h) => { calls++; return a + b + c + d + e + f + g + h; });
        Assert.Equal(0, calls);
        Assert.Equal(new[] { "e8" }, r.Error);
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
    public void AggregateEnumerable_Mixed_ReturnsOnlyErrorsInInputOrder()
    {
        var results = new List<Result<int, string>> { 1, "e1", 2, 3, "e2", 4 };

        var r = Result.Aggregate(results);
        Assert.Equal(new[] { "e1", "e2" }, r.Error);
    }

    [Fact]
    public void AggregateEnumerable_Empty_ReturnsEmptySuccessList()
    {
        var r = Result.Aggregate(new List<Result<int, string>>());
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

    // ── Round-trip: combiner ≡ tuple form followed by Map ──────────────

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
