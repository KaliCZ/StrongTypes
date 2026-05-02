using System.ComponentModel.DataAnnotations;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringCollectionTests
{
    [Property]
    public void Count_MatchesValueLength(NonEmptyString s) =>
        Assert.Equal(s.Value.Length, s.Count);

    [Property]
    public void Indexer_MatchesValueIndexer(NonEmptyString s)
    {
        for (var i = 0; i < s.Value.Length; i++)
        {
            Assert.Equal(s.Value[i], s[i]);
        }
    }

    // Load-bearing per the issue: BCL [MaxLength] reflects on Count after the
    // `value is string` check fails. Adding Count makes the bare BCL attribute
    // work without consumers shipping a custom shim.
    [Fact]
    public void MaxLengthAttribute_WiredToCount_PassesWhenWithinLimit()
    {
        var attribute = new MaxLengthAttribute(10);
        Assert.True(attribute.IsValid(NonEmptyString.Create("short")));
    }

    [Fact]
    public void MaxLengthAttribute_WiredToCount_FailsWhenOverLimit()
    {
        var attribute = new MaxLengthAttribute(5);
        Assert.False(attribute.IsValid(NonEmptyString.Create("too-long-value")));
    }
}
