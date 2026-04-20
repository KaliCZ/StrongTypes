#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// <see cref="JsonConverterFactory"/> for the concrete <see cref="NonEmptyEnumerable{T}"/>.
/// Reads a JSON array and fails with a <see cref="JsonException"/> when the array is empty
/// or the JSON token is not an array; JSON null round-trips to C# null. Writes as a JSON array.
/// </summary>
/// <remarks>
/// Only the concrete class is registered, not <see cref="INonEmptyEnumerable{T}"/>:
/// <c>System.Text.Json</c> matches converters by the exact declared type, so returning a
/// <c>JsonConverter&lt;NonEmptyEnumerable&lt;T&gt;&gt;</c> for an interface-typed property wouldn't bind.
/// </remarks>
public sealed class NonEmptyEnumerableJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(NonEmptyEnumerable<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            var elementType = t.GetGenericArguments()[0];
            var converterType = typeof(Inner<>).MakeGenericType(elementType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });

    private sealed class Inner<T> : JsonConverter<NonEmptyEnumerable<T>?>
    {
        // HandleNull so STJ hands us the JSON null token instead of short-circuiting
        // to default(NonEmptyEnumerable<T>?) — we want "null in, null out" semantics.
        public override bool HandleNull => true;

        public override NonEmptyEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected StartArray for {typeof(NonEmptyEnumerable<T>).Name}, got {reader.TokenType}.");

            var buffer = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    if (buffer.Count == 0)
                        throw new JsonException($"The JSON array is empty and cannot be converted to {typeof(NonEmptyEnumerable<T>).Name}.");
                    return NonEmptyEnumerable<T>.FromValidatedArray(CollectionsMarshal.AsSpan(buffer).ToArray());
                }

                var element = JsonSerializer.Deserialize<T>(ref reader, options);
                // Null on the wire for a reference-typed element violates the invariant —
                // the type guarantees its contents are well-formed, not just non-empty.
                if (element is null && default(T) is null)
                    throw new JsonException($"Null element in {typeof(NonEmptyEnumerable<T>).Name} is not allowed.");

                buffer.Add(element!);
            }

            throw new JsonException($"Unexpected end of JSON while reading {typeof(NonEmptyEnumerable<T>).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, NonEmptyEnumerable<T>? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (var element in value.AsSpan())
                JsonSerializer.Serialize(writer, element, options);
            writer.WriteEndArray();
        }
    }
}
