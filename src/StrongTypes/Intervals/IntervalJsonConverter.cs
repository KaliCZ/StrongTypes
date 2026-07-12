#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A <see cref="JsonConverter{T}"/> for a single interval type that pins each bound's inclusivity via <see cref="IntervalBoundMode"/>.
/// The built-in <see cref="IntervalJsonConverterFactory"/> already serializes every interval with per-value
/// (<see cref="IntervalBoundMode.Stored"/>) bounds; reach for this only to fix a bound on a specific property or options set —
/// either by adding an instance to <see cref="JsonSerializerOptions.Converters"/>, or by subclassing with a parameterless
/// constructor and applying <c>[JsonConverter(typeof(YourConverter))]</c>:
/// <code>
/// public sealed class HalfOpenIntervalConverter()
///     : IntervalJsonConverter&lt;Interval&lt;int&gt;&gt;(IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysExclusive);
/// </code>
/// </summary>
/// <typeparam name="TInterval">One of <see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>.</typeparam>
/// <remarks>An <see cref="IntervalBoundMode.AlwaysInclusive"/> or <see cref="IntervalBoundMode.AlwaysExclusive"/> bound is never written and is forced to the configured inclusivity on read; a value whose bound contradicts the mode throws <see cref="JsonException"/> when written.</remarks>
public class IntervalJsonConverter<TInterval> : JsonConverter<TInterval>
    where TInterval : struct
{
    private readonly JsonConverter<TInterval> _inner;

    /// <summary>Serializes both bounds per value — the same behavior as the built-in factory.</summary>
    public IntervalJsonConverter() : this(IntervalBoundMode.Stored, IntervalBoundMode.Stored)
    {
    }

    /// <param name="startMode">How the start bound's inclusivity is represented on the wire.</param>
    /// <param name="endMode">How the end bound's inclusivity is represented on the wire.</param>
    public IntervalJsonConverter(IntervalBoundMode startMode, IntervalBoundMode endMode) =>
        _inner = IntervalJsonConverterFactory.CreateBoundConverter<TInterval>(startMode, endMode);

    public sealed override TInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _inner.Read(ref reader, typeToConvert, options);

    public sealed override void Write(Utf8JsonWriter writer, TInterval value, JsonSerializerOptions options) =>
        _inner.Write(writer, value, options);
}
