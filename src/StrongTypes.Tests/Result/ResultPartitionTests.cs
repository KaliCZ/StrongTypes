using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultPartitionTests
{
    [Fact]
    public void Partition_Empty_ReturnsEmptyLists()
    {
        var (s, e) = System.Array.Empty<Result<int, string>>().Partition();
        Assert.Empty(s);
        Assert.Empty(e);
    }

    [Fact]
    public void Partition_MixedSequence_SplitsAndPreservesOrder()
    {
        var items = new Result<int, string>[] { 1, "a", 2, "b", 3 };
        var (successes, errors) = items.Partition();
        Assert.Equal(new[] { 1, 2, 3 }, successes);
        Assert.Equal(new[] { "a", "b" }, errors);
    }

    [Property]
    public void Partition_PreservesTotalCount(Result<int, string>[] items)
    {
        var (successes, errors) = items.Partition();
        Assert.Equal(items.Length, successes.Count + errors.Count);
    }

    [Property]
    public void Partition_EverySuccessAppearsOncePerSource(Result<int, string>[] items)
    {
        var (successes, _) = items.Partition();
        var expected = items.Where(r => r.IsSuccess).Select(r => (int)r.Success!);
        Assert.Equal(expected, successes);
    }

    [Property]
    public void Partition_EveryErrorAppearsOncePerSource(Result<int, string>[] items)
    {
        var (_, errors) = items.Partition();
        var expected = items.Where(r => r.IsError).Select(r => r.Error!);
        Assert.Equal(expected, errors);
    }

    // ── PartitionMatch (void) ──────────────────────────────────────────

    [Fact]
    public void PartitionMatch_Void_InvokesBothCallbacks()
    {
        var items = new Result<int, string>[] { 1, "a" };
        IReadOnlyList<int>? successes = null;
        IReadOnlyList<string>? errors = null;
        items.PartitionMatch(
            success: ss => successes = ss,
            error: es => errors = es);
        Assert.Equal(new[] { 1 }, successes);
        Assert.Equal(new[] { "a" }, errors);
    }

    [Fact]
    public void PartitionMatch_Void_EmptyInput_StillInvokesBothCallbacks()
    {
        var invocations = 0;
        System.Array.Empty<Result<int, string>>().PartitionMatch(
            success: _ => invocations++,
            error: _ => invocations++);
        Assert.Equal(2, invocations);
    }

    // ── PartitionMatch (projection) ────────────────────────────────────

    [Fact]
    public void PartitionMatch_Projection_ConcatenatesSuccessesThenErrors()
    {
        var items = new Result<int, string>[] { 1, "a", 2, "b" };
        var projected = items.PartitionMatch(
            success: ss => ss.Select(s => $"ok:{s}"),
            error: es => es.Select(e => $"err:{e}"));
        Assert.Equal(new[] { "ok:1", "ok:2", "err:a", "err:b" }, projected);
    }
}
