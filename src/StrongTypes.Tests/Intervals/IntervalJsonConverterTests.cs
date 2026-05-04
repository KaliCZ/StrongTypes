using System.Text.Json;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalJsonConverterTests
{
    [Property]
    public void ClosedInterval_RoundTrips(ClosedInterval<int> interval)
    {
        var json = JsonSerializer.Serialize(interval);
        var roundTripped = JsonSerializer.Deserialize<ClosedInterval<int>>(json);
        Assert.Equal(interval, roundTripped);
    }

    [Property]
    public void Interval_RoundTrips(Interval<int> interval)
    {
        var json = JsonSerializer.Serialize(interval);
        var roundTripped = JsonSerializer.Deserialize<Interval<int>>(json);
        Assert.Equal(interval, roundTripped);
    }

    [Property]
    public void IntervalFrom_RoundTrips(IntervalFrom<int> interval)
    {
        var json = JsonSerializer.Serialize(interval);
        var roundTripped = JsonSerializer.Deserialize<IntervalFrom<int>>(json);
        Assert.Equal(interval, roundTripped);
    }

    [Property]
    public void IntervalUntil_RoundTrips(IntervalUntil<int> interval)
    {
        var json = JsonSerializer.Serialize(interval);
        var roundTripped = JsonSerializer.Deserialize<IntervalUntil<int>>(json);
        Assert.Equal(interval, roundTripped);
    }

    [Fact]
    public void ClosedInterval_RejectsInvalidPayload()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ClosedInterval<int>>("""{"Start":5,"End":1}"""));
    }

    [Fact]
    public void Interval_AcceptsAllNullableShapes()
    {
        var bothNull = JsonSerializer.Deserialize<Interval<int>>("""{"Start":null,"End":null}""");
        Assert.Null(bothNull.Start);
        Assert.Null(bothNull.End);

        var startOnly = JsonSerializer.Deserialize<Interval<int>>("""{"Start":1,"End":null}""");
        Assert.Equal(1, startOnly.Start);
        Assert.Null(startOnly.End);

        var endOnly = JsonSerializer.Deserialize<Interval<int>>("""{"Start":null,"End":10}""");
        Assert.Null(endOnly.Start);
        Assert.Equal(10, endOnly.End);

        var both = JsonSerializer.Deserialize<Interval<int>>("""{"Start":1,"End":10}""");
        Assert.Equal(1, both.Start);
        Assert.Equal(10, both.End);
    }

    [Fact]
    public void IntervalFrom_RejectsNullStart()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IntervalFrom<int>>("""{"Start":null,"End":10}"""));
    }

    [Fact]
    public void IntervalUntil_RejectsNullEnd()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IntervalUntil<int>>("""{"Start":1,"End":null}"""));
    }

    [Fact]
    public void Write_EmitsExpectedShape()
    {
        var interval = ClosedInterval<int>.Create(1, 10);
        var json = JsonSerializer.Serialize(interval);
        Assert.Equal("""{"Start":1,"End":10}""", json);
    }

    [Fact]
    public void NamingPolicy_IsHonoured()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var interval = ClosedInterval<int>.Create(1, 10);
        var json = JsonSerializer.Serialize(interval, options);
        Assert.Equal("""{"start":1,"end":10}""", json);

        var roundTripped = JsonSerializer.Deserialize<ClosedInterval<int>>(json, options);
        Assert.Equal(interval, roundTripped);
    }
}
