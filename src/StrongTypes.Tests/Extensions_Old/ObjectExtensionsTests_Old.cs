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

    [Fact]
    public void AsCoproduct()
    {
        var c1 = "foo".AsCoproduct<string, int>();
        Assert.True(c1.First.IsSome);
        Assert.Equal("foo", c1.First.Value);

        var c2 = 42.AsCoproduct<string, int>();
        Assert.True(c2.Second.IsSome);
        Assert.Equal(42, c2.Second.Value);

        var c3 = 42.AsCoproduct<int, int>();
        Assert.True(c3.First.IsSome);
        Assert.Equal(42, c3.First.Value);

        Assert.Throws<ArgumentException>(() => new object().AsCoproduct<string, int>());

        var c4 = "foo".AsCoproduct("foo", "bar");
        Assert.True(c4.First.IsSome);
        Assert.Equal("foo", c4.First.Value);

        var c5 = "foo".AsCoproduct("bar", "foo");
        Assert.True(c5.Second.IsSome);
        Assert.Equal("foo", c5.Second.Value);

        Assert.Throws<ArgumentException>(() => new object().AsCoproduct("foo", "bar"));
    }

    [Fact]
    public void AsSafeCoproduct()
    {
        var c1 = "foo".AsSafeCoproduct<string, int>();
        Assert.True(c1.First.IsSome);
        Assert.Equal("foo", c1.First.Value);

        var c2 = 42.AsSafeCoproduct<string, int>();
        Assert.True(c2.Second.IsSome);
        Assert.Equal(42, c2.Second.Value);

        var c3 = 42.AsSafeCoproduct<int, int>();
        Assert.True(c3.First.IsSome);
        Assert.Equal(42, c3.First.Value);

        var c4 = "foo".AsSafeCoproduct<int, int>();
        Assert.True(c4.Third.IsSome);
        Assert.Equal("foo", c4.Third.Value);

        var c5 = "foo".AsSafeCoproduct("foo", "bar");
        Assert.True(c5.First.IsSome);
        Assert.Equal("foo", c5.First.Value);

        var c6 = "foo".AsSafeCoproduct("bar", "foo");
        Assert.True(c6.Second.IsSome);
        Assert.Equal("foo", c6.Second.Value);

        var c7 = "foo".AsSafeCoproduct("bar", "baz");
        Assert.True(c7.Third.IsSome);
        Assert.Equal("foo", c7.Third.Value);
    }
}