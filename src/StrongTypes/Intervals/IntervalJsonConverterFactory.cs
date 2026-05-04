#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary><see cref="JsonConverterFactory"/> for the four interval types: <see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>.</summary>
/// <remarks>Each interval is read and written as a JSON object with <c>Start</c> and <c>End</c> properties; values that violate the wrapper's invariant throw <see cref="JsonException"/>. Property names honour the active <see cref="JsonNamingPolicy"/>.</remarks>
public sealed class IntervalJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedDefinitions =
    [
        typeof(ClosedInterval<>),
        typeof(Interval<>),
        typeof(IntervalFrom<>),
        typeof(IntervalUntil<>)
    ];

    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && SupportedDefinitions.Contains(typeToConvert.GetGenericTypeDefinition());

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            var definition = t.GetGenericTypeDefinition();
            var endpoint = t.GetGenericArguments()[0];
            var (startType, endType) = StartEndTypes(definition, endpoint);
            var converterType = typeof(Inner<,,>).MakeGenericType(t, startType, endType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });

    private static (Type Start, Type End) StartEndTypes(Type definition, Type endpoint)
    {
        var nullable = typeof(Nullable<>).MakeGenericType(endpoint);
        if (definition == typeof(ClosedInterval<>)) return (endpoint, endpoint);
        if (definition == typeof(Interval<>)) return (nullable, nullable);
        if (definition == typeof(IntervalFrom<>)) return (endpoint, nullable);
        if (definition == typeof(IntervalUntil<>)) return (nullable, endpoint);
        throw new InvalidOperationException($"Unsupported interval definition: {definition}.");
    }

    private sealed class Inner<TInterval, TStart, TEnd> : JsonConverter<TInterval>
        where TInterval : struct
    {
        private static readonly Func<TInterval, TStart> s_getStart = BuildGetter<TStart>("Start");
        private static readonly Func<TInterval, TEnd> s_getEnd = BuildGetter<TEnd>("End");
        private static readonly Func<TStart, TEnd, TInterval?> s_tryCreate = BuildTryCreate();

        private static Func<TInterval, TProp> BuildGetter<TProp>(string propertyName)
        {
            var param = Expression.Parameter(typeof(TInterval), "interval");
            var access = Expression.Property(param, propertyName);
            return Expression.Lambda<Func<TInterval, TProp>>(access, param).Compile();
        }

        private static Func<TStart, TEnd, TInterval?> BuildTryCreate()
        {
            var start = Expression.Parameter(typeof(TStart), "start");
            var end = Expression.Parameter(typeof(TEnd), "end");
            var method = typeof(TInterval).GetMethod(
                "TryCreate", BindingFlags.Public | BindingFlags.Static, [typeof(TStart), typeof(TEnd)])!;
            var call = Expression.Call(method, start, end);
            return Expression.Lambda<Func<TStart, TEnd, TInterval?>>(call, start, end).Compile();
        }

        public override TInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object for {typeof(TInterval).Name}.");
            }

            var startName = NameOf("Start", options);
            var endName = NameOf("End", options);
            TStart start = default!;
            TEnd end = default!;
            var sawStart = false;
            var sawEnd = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (!sawStart || !sawEnd)
                    {
                        throw new JsonException($"{typeof(TInterval).Name} requires both '{startName}' and '{endName}' properties.");
                    }
                    return s_tryCreate(start, end)
                        ?? throw new JsonException($"The JSON value cannot be converted to {typeof(TInterval).Name}: invariant violated.");
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Unexpected token while reading {typeof(TInterval).Name}.");
                }

                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, startName, StringComparison.Ordinal))
                {
                    start = JsonSerializer.Deserialize<TStart>(ref reader, options)!;
                    sawStart = true;
                }
                else if (string.Equals(name, endName, StringComparison.Ordinal))
                {
                    end = JsonSerializer.Deserialize<TEnd>(ref reader, options)!;
                    sawEnd = true;
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException($"Unterminated JSON object while reading {typeof(TInterval).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TInterval value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(NameOf("Start", options));
            JsonSerializer.Serialize(writer, s_getStart(value), options);
            writer.WritePropertyName(NameOf("End", options));
            JsonSerializer.Serialize(writer, s_getEnd(value), options);
            writer.WriteEndObject();
        }

        private static string NameOf(string clrName, JsonSerializerOptions options) =>
            options.PropertyNamingPolicy?.ConvertName(clrName) ?? clrName;
    }
}
