using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongTypes.Tests;

public class ObjectExtensionsTests
{
    [Fact]
    public void Match()
    {
        Assert.Equal("foo", 0.Match(
            0, _ => "foo",
            1, _ => "bar"
        ));
        Assert.Equal("bar", 1.Match(
            0, _ => "foo",
            1, _ => "bar"
        ));
        Assert.Equal("baz", 2.Match(
            0, _ => "foo",
            1, _ => "bar",
            _ => "baz"
        ));
        Assert.True(DateTimeKind.Utc.Match(DateTimeKind.Utc, _ => true));
        Assert.Throws<ArgumentException>(() => 2.Match(
            0, _ => "foo",
            1, _ => "bar"
        ));
    }
        
    [Fact]
    public async Task MatchAsync()
    {
        Assert.Equal("foo", await 0.MatchAsync(
            0, _ => Task.FromResult("foo"),
            1, _ => Task.FromResult("bar")
        ));
            
        Assert.Equal("baz", await 2.MatchAsync(
            0, _ => Task.FromResult("foo"),
            1, _ => Task.FromResult("bar"),
            _ => Task.FromResult("baz")
        ));

        await Assert.ThrowsAsync<ArgumentException>(async () => await 2.MatchAsync(
            0, _ => Task.FromResult("foo"),
            1, _ => Task.FromResult("bar")
        ));
    }
}