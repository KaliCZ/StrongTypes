using System;
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

    [Fact]
    public void Read_OmittedRequiredEndpoint_Throws()
    {
        // ClosedInterval requires both endpoints.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ClosedInterval<int>>("""{"Start":1}"""));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ClosedInterval<int>>("""{"End":1}"""));
        // IntervalFrom requires Start; IntervalUntil requires End.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IntervalFrom<int>>("""{"End":10}"""));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IntervalUntil<int>>("""{"Start":1}"""));
    }

    [Fact]
    public void Read_OmittedOptionalEndpoint_DefaultsToNull()
    {
        // Both endpoints optional: an empty object is the unbounded interval.
        var open = JsonSerializer.Deserialize<Interval<int>>("{}");
        Assert.Null(open.Start);
        Assert.Null(open.End);

        // Omitting the optional upper bound is open-ended.
        var from = JsonSerializer.Deserialize<IntervalFrom<int>>("""{"Start":1}""");
        Assert.Equal(1, from.Start);
        Assert.Null(from.End);

        // Omitting the optional lower bound is open-started.
        var until = JsonSerializer.Deserialize<IntervalUntil<int>>("""{"End":10}""");
        Assert.Null(until.Start);
        Assert.Equal(10, until.End);
    }

    [Fact]
    public void Read_NonObjectToken_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClosedInterval<int>>("5"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClosedInterval<int>>("[1,10]"));
    }

    [Fact]
    public void Read_IgnoresUnknownProperties()
    {
        var interval = JsonSerializer.Deserialize<ClosedInterval<int>>(
            """{"Start":1,"Unknown":{"nested":true},"End":10,"Extra":42}""");
        Assert.Equal(ClosedInterval<int>.Create(1, 10), interval);
    }

    [Fact]
    public void Read_PropertyOrderDoesNotMatter()
    {
        var interval = JsonSerializer.Deserialize<ClosedInterval<int>>("""{"End":10,"Start":1}""");
        Assert.Equal(ClosedInterval<int>.Create(1, 10), interval);
    }

    // Endpoints are any IComparable<T> struct, not just int. A DateOnly range is the
    // canonical real-world case and proves the endpoint's own converter (date format)
    // composes with the interval converter.
    [Fact]
    public void DateOnly_RoundTrips()
    {
        var interval = ClosedInterval<DateOnly>.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var json = JsonSerializer.Serialize(interval);
        Assert.Equal("""{"Start":"2026-01-01","End":"2026-12-31"}""", json);
        Assert.Equal(interval, JsonSerializer.Deserialize<ClosedInterval<DateOnly>>(json));
    }

    [Fact]
    public void DateTimeInterval_OpenEnded_RoundTrips()
    {
        var from = IntervalFrom<DateTime>.Create(new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc), null);
        var roundTripped = JsonSerializer.Deserialize<IntervalFrom<DateTime>>(JsonSerializer.Serialize(from));
        Assert.Equal(from, roundTripped);
    }

    // The client-facing message must not leak the arity-suffixed CLR name
    // ("ClosedInterval`1") — see IntervalJsonConverterFactory.
    [Fact]
    public void InvariantViolation_Message_IsHumanReadable()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ClosedInterval<int>>("""{"Start":5,"End":1}"""));
        Assert.Contains("ClosedInterval", ex.Message);
        Assert.DoesNotContain("`", ex.Message);
    }

    // A type mismatch in an endpoint is rethrown path-less (inner exception
    // preserved) so System.Text.Json can reattach the property path — the fix
    // that keeps the API error key at "$.value" rather than the document root.
    [Fact]
    public void EndpointTypeMismatch_ThrowsJsonExceptionPreservingInner()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ClosedInterval<int>>("""{"Start":1,"End":"not-a-number"}"""));
        Assert.NotNull(ex.InnerException);
    }
}
