#nullable enable

using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class TryPartitionTests
{
    private static IEnumerable<Try<int, string>> Build(int[] successes, string[] errors, bool successFirst)
    {
        var s = successes.Select(i => Try.Success<int, string>(i));
        var e = errors.Select(msg => Try.Error<int, string>(msg ?? ""));
        return successFirst ? s.Concat(e) : e.Concat(s);
    }

    [Property]
    public void Partition_SplitsByIsSuccess(int[] successes, string[] errors)
    {
        var (s, e) = Build(successes, errors, successFirst: true).Partition();
        Assert.Equal(successes, s);
        Assert.Equal(errors.Select(x => x ?? ""), e);
    }

    [Property]
    public void Partition_PreservesRelativeOrder(int[] successes, string[] errors)
    {
        var (s, e) = Build(successes, errors, successFirst: false).Partition();
        Assert.Equal(successes, s);
        Assert.Equal(errors.Select(x => x ?? ""), e);
    }

    [Fact]
    public void Partition_AllErrors_SuccessesEmpty()
    {
        var source = new[]
        {
            Try.Error<int, string>("a"),
            Try.Error<int, string>("b"),
        };
        var (s, e) = source.Partition();
        Assert.Empty(s);
        Assert.Equal(new[] { "a", "b" }, e);
    }

    [Fact]
    public void Partition_AllSuccesses_ErrorsEmpty()
    {
        var source = new[]
        {
            Try.Success<int, string>(1),
            Try.Success<int, string>(2),
        };
        var (s, e) = source.Partition();
        Assert.Equal(new[] { 1, 2 }, s);
        Assert.Empty(e);
    }

    [Fact]
    public void Partition_Empty_ReturnsTwoEmptyLists()
    {
        var (s, e) = System.Array.Empty<Try<int, string>>().Partition();
        Assert.Empty(s);
        Assert.Empty(e);
    }

    [Property]
    public void PartitionMatch_Void_InvokesBothWithCorrectPartitions(int[] successes, string[] errors)
    {
        IReadOnlyList<int>? seenSuccesses = null;
        IReadOnlyList<string>? seenErrors = null;

        Build(successes, errors, successFirst: true).PartitionMatch(
            s => seenSuccesses = s,
            e => seenErrors = e);

        Assert.NotNull(seenSuccesses);
        Assert.NotNull(seenErrors);
        Assert.Equal(successes, seenSuccesses);
        Assert.Equal(errors.Select(x => x ?? ""), seenErrors);
    }

    [Fact]
    public void PartitionMatch_Void_InvokesBothEvenWhenEmpty()
    {
        var successCalled = 0;
        var errorCalled = 0;
        System.Array.Empty<Try<int, string>>().PartitionMatch(
            _ => successCalled++,
            _ => errorCalled++);

        Assert.Equal(1, successCalled);
        Assert.Equal(1, errorCalled);
    }

    [Property]
    public void PartitionMatch_Projecting_ConcatsSuccessesThenErrors(int[] successes, string[] errors)
    {
        var result = Build(successes, errors, successFirst: true).PartitionMatch(
            s => s.Select(i => $"s:{i}"),
            e => e.Select(msg => $"e:{msg}"));

        var expected = successes.Select(i => $"s:{i}")
            .Concat(errors.Select(x => $"e:{x ?? ""}"))
            .ToArray();
        Assert.Equal(expected, result);
    }
}
