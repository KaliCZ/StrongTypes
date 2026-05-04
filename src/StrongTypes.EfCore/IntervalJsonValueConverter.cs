using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips an interval through a single JSON-encoded <see cref="string"/> column. Supports <see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, and <see cref="IntervalUntil{T}"/>.</summary>
/// <typeparam name="TInterval">The interval type.</typeparam>
public sealed class IntervalJsonValueConverter<TInterval> : ValueConverter<TInterval, string>
    where TInterval : struct
{
    private static readonly JsonSerializerOptions s_options = new();

    public IntervalJsonValueConverter()
        : base(
            v => JsonSerializer.Serialize(v, s_options),
            s => JsonSerializer.Deserialize<TInterval>(s, s_options)!)
    {
    }
}
