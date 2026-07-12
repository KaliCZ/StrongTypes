using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace StrongTypes.Tests;

public class IntervalJsonConverterModeTests
{
    // The intended usage: a one-line subclass passing the two modes to the base.
    private sealed class HalfOpenFiniteConverter()
        : IntervalJsonConverter<FiniteInterval<int>>(IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysExclusive);

    private sealed class Booking
    {
        [JsonConverter(typeof(HalfOpenFiniteConverter))]
        public FiniteInterval<int> Window { get; set; }
    }

    private static JsonSerializerOptions With(JsonConverter converter) => new() { Converters = { converter } };

    [Fact]
    public void FixedModes_OmitBothFlagsOnWrite()
    {
        var json = JsonSerializer.Serialize(FiniteInterval.Create(1, 10, endInclusive: false), With(new HalfOpenFiniteConverter()));
        Assert.Equal("""{"Start":1,"End":10}""", json);
    }

    [Fact]
    public void AlwaysExclusiveEnd_ForcesExclusiveOnRead()
    {
        var interval = JsonSerializer.Deserialize<FiniteInterval<int>>("""{"Start":1,"End":10}""", With(new HalfOpenFiniteConverter()));
        Assert.True(interval.StartInclusive);
        Assert.False(interval.EndInclusive);
    }

    [Fact]
    public void PinnedBound_IgnoresAContradictingPayloadFlagOnRead()
    {
        var interval = JsonSerializer.Deserialize<FiniteInterval<int>>(
            """{"Start":1,"End":10,"EndInclusive":true}""", With(new HalfOpenFiniteConverter()));
        Assert.False(interval.EndInclusive);
    }

    [Fact]
    public void PinnedBound_RejectsAContradictingValueOnWrite()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Serialize(FiniteInterval.Create(1, 10), With(new HalfOpenFiniteConverter())));
        Assert.Contains("always-exclusive", ex.Message);
    }

    [Fact]
    public void AlwaysExclusive_EqualEndpointsRejectedOnRead()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FiniteInterval<int>>("""{"Start":5,"End":5}""", With(new HalfOpenFiniteConverter())));
    }

    [Fact]
    public void AppliedViaAttribute_RoundTripsThroughTheProperty()
    {
        var json = JsonSerializer.Serialize(new Booking { Window = FiniteInterval.Create(1, 10, endInclusive: false) });
        Assert.Equal("""{"Window":{"Start":1,"End":10}}""", json);

        var back = JsonSerializer.Deserialize<Booking>("""{"Window":{"Start":1,"End":10}}""");
        Assert.Equal(FiniteInterval.Create(1, 10, endInclusive: false), back!.Window);
    }

    [Fact]
    public void MixedMode_StoredStartCarriesPerValue_FixedEndPinned()
    {
        var converter = new IntervalJsonConverter<FiniteInterval<int>>(IntervalBoundMode.Stored, IntervalBoundMode.AlwaysExclusive);

        Assert.Equal(
            """{"Start":1,"End":10,"StartInclusive":false}""",
            JsonSerializer.Serialize(FiniteInterval.Create(1, 10, startInclusive: false, endInclusive: false), With(converter)));

        var back = JsonSerializer.Deserialize<FiniteInterval<int>>("""{"Start":1,"End":10,"StartInclusive":false}""", With(converter));
        Assert.False(back.StartInclusive);
        Assert.False(back.EndInclusive);
    }

    [Fact]
    public void PinnedBound_ToleratesAnUnboundedEndpoint()
    {
        var converter = new IntervalJsonConverter<Interval<int>>(IntervalBoundMode.AlwaysExclusive, IntervalBoundMode.AlwaysExclusive);
        var value = Interval.Create(1, (int?)null, startInclusive: false);

        var json = JsonSerializer.Serialize(value, With(converter));
        Assert.Equal("""{"Start":1,"End":null}""", json);
        Assert.Equal(value, JsonSerializer.Deserialize<Interval<int>>(json, With(converter)));
    }
}
