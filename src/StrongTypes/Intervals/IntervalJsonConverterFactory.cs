#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary><see cref="JsonConverterFactory"/> for the four interval types: <see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>.</summary>
/// <remarks>Each interval is read and written as a JSON object with <c>Start</c> and <c>End</c> properties. <c>StartInclusive</c> and <c>EndInclusive</c> are written only when <c>false</c> and read as <c>true</c> when absent. Values that violate the wrapper's invariant throw <see cref="JsonException"/>. Property names honour the active <see cref="JsonNamingPolicy"/>.</remarks>
public sealed class IntervalJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedDefinitions =
    [
        typeof(FiniteInterval<>),
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
        if (definition == typeof(FiniteInterval<>)) return (endpoint, endpoint);
        if (definition == typeof(Interval<>)) return (nullable, nullable);
        if (definition == typeof(IntervalFrom<>)) return (endpoint, nullable);
        if (definition == typeof(IntervalUntil<>)) return (nullable, endpoint);
        throw new InvalidOperationException($"Unsupported interval definition: {definition}.");
    }

    private sealed class Inner<TInterval, TStart, TEnd> : JsonConverter<TInterval>
        where TInterval : struct
    {
        // The arity-suffixed CLR name ("FiniteInterval`1") would leak into client-facing validation errors.
        private static readonly string s_name = typeof(TInterval).Name.Split('`')[0];

        // An endpoint is required exactly when its type is the bare value type;
        // an optional endpoint is Nullable<T>, so omitting its key means "null".
        private static readonly bool s_startRequired = Nullable.GetUnderlyingType(typeof(TStart)) is null;
        private static readonly bool s_endRequired = Nullable.GetUnderlyingType(typeof(TEnd)) is null;

        private static readonly Func<TInterval, TStart> s_getStart = BuildGetter<TStart>("Start");
        private static readonly Func<TInterval, TEnd> s_getEnd = BuildGetter<TEnd>("End");
        private static readonly Func<TInterval, bool> s_getStartInclusive = BuildGetter<bool>("StartInclusive");
        private static readonly Func<TInterval, bool> s_getEndInclusive = BuildGetter<bool>("EndInclusive");
        private static readonly Func<TStart, TEnd, bool, bool, TInterval?> s_tryCreate = BuildTryCreate();

        private static Func<TInterval, TProp> BuildGetter<TProp>(string propertyName)
        {
            var param = Expression.Parameter(typeof(TInterval), "interval");
            var access = Expression.Property(param, propertyName);
            return Expression.Lambda<Func<TInterval, TProp>>(access, param).Compile();
        }

        private static Func<TStart, TEnd, bool, bool, TInterval?> BuildTryCreate()
        {
            var start = Expression.Parameter(typeof(TStart), "start");
            var end = Expression.Parameter(typeof(TEnd), "end");
            var startInclusive = Expression.Parameter(typeof(bool), "startInclusive");
            var endInclusive = Expression.Parameter(typeof(bool), "endInclusive");
            var method = typeof(TInterval).GetMethod(
                "TryCreate", BindingFlags.Public | BindingFlags.Static, [typeof(TStart), typeof(TEnd), typeof(bool), typeof(bool)])!;
            var call = Expression.Call(method, start, end, startInclusive, endInclusive);
            return Expression.Lambda<Func<TStart, TEnd, bool, bool, TInterval?>>(call, start, end, startInclusive, endInclusive).Compile();
        }

        public override TInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected a JSON object with '{NameOf("Start", options)}' and '{NameOf("End", options)}' properties for {s_name}.");
            }

            var startName = NameOf("Start", options);
            var endName = NameOf("End", options);
            var startInclusiveName = NameOf("StartInclusive", options);
            var endInclusiveName = NameOf("EndInclusive", options);
            TStart start = default!;
            TEnd end = default!;
            var startInclusive = true;
            var endInclusive = true;
            var sawStart = false;
            var sawEnd = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    // A missing key is fine for an optional endpoint (it stays null);
                    // only a required endpoint must be present.
                    if (!sawStart && s_startRequired)
                    {
                        throw new JsonException($"{s_name} requires the '{startName}' property.");
                    }
                    if (!sawEnd && s_endRequired)
                    {
                        throw new JsonException($"{s_name} requires the '{endName}' property.");
                    }
                    return s_tryCreate(start, end, startInclusive, endInclusive)
                        ?? throw new JsonException(
                            $"{s_name} requires '{startName}' to be less than or equal to '{endName}', with equal endpoints only when both are inclusive.");
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Unexpected token while reading {s_name}.");
                }

                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, startName, StringComparison.Ordinal))
                {
                    start = ReadEndpoint<TStart>(ref reader, options, startName);
                    sawStart = true;
                }
                else if (string.Equals(name, endName, StringComparison.Ordinal))
                {
                    end = ReadEndpoint<TEnd>(ref reader, options, endName);
                    sawEnd = true;
                }
                else if (string.Equals(name, startInclusiveName, StringComparison.Ordinal))
                {
                    startInclusive = ReadEndpoint<bool>(ref reader, options, startInclusiveName);
                }
                else if (string.Equals(name, endInclusiveName, StringComparison.Ordinal))
                {
                    endInclusive = ReadEndpoint<bool>(ref reader, options, endInclusiveName);
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException($"Unterminated JSON object while reading {s_name}.");
        }

        // The nested Deserialize loses the property position, so a failure (null
        // for a required endpoint, a type mismatch) surfaces with the document
        // root as its path. Rethrow path-less and the serializer reattaches the
        // correct path (e.g. "$.value") — matching the numeric converter and the
        // error-key contract codified in #106.
        private static TEndpoint ReadEndpoint<TEndpoint>(
            ref Utf8JsonReader reader, JsonSerializerOptions options, string property)
        {
            try
            {
                return JsonSerializer.Deserialize<TEndpoint>(ref reader, options)!;
            }
            catch (JsonException ex)
            {
                throw new JsonException($"The JSON value for '{property}' could not be converted for {s_name}.", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, TInterval value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(NameOf("Start", options));
            JsonSerializer.Serialize(writer, s_getStart(value), options);
            writer.WritePropertyName(NameOf("End", options));
            JsonSerializer.Serialize(writer, s_getEnd(value), options);
            if (!s_getStartInclusive(value))
            {
                writer.WriteBoolean(NameOf("StartInclusive", options), false);
            }
            if (!s_getEndInclusive(value))
            {
                writer.WriteBoolean(NameOf("EndInclusive", options), false);
            }
            writer.WriteEndObject();
        }

        private static string NameOf(string clrName, JsonSerializerOptions options) =>
            options.PropertyNamingPolicy?.ConvertName(clrName) ?? clrName;
    }
}
