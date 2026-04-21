#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// <see cref="JsonConverterFactory"/> for <see cref="NonEmptyEnumerable{T}"/> and its
/// <see cref="INonEmptyEnumerable{T}"/> interface. Reads a JSON array and fails with a
/// <see cref="JsonException"/> when the array is empty or the JSON token is not an array;
/// JSON null round-trips to C# null. Writes as a JSON array.
/// </summary>
/// <remarks>
/// Both the concrete class and the interface match: the class converter returns
/// <c>NonEmptyEnumerable&lt;T&gt;?</c>, the interface converter returns
/// <c>INonEmptyEnumerable&lt;T&gt;?</c> (still built from a <see cref="NonEmptyEnumerable{T}"/>
/// instance). Two converters are needed because <c>System.Text.Json</c> matches by exact
/// declared type — one <c>JsonConverter&lt;X&gt;</c> can't serve both <c>X</c> and an interface
/// that <c>X</c> implements.
/// </remarks>
public sealed class NonEmptyEnumerableJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        var def = typeToConvert.GetGenericTypeDefinition();
        return def == typeof(NonEmptyEnumerable<>) || def == typeof(INonEmptyEnumerable<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            var elementType = t.GetGenericArguments()[0];
            var isInterface = t.GetGenericTypeDefinition() == typeof(INonEmptyEnumerable<>);
            var converterType = (isInterface ? typeof(InterfaceInner<>) : typeof(Inner<>))
                .MakeGenericType(elementType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });

    // Shared read/write logic — both converters deserialize into the concrete type and
    // serialize by iterating a non-empty sequence. Kept here so Inner/InterfaceInner
    // stay tiny type-only adapters.
    private static NonEmptyEnumerable<T>? ReadCore<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
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

            // The non-empty invariant is about Count, not element content — mirrors the
            // factories, which also let nulls through. If T itself forbids null (e.g. a
            // struct, or a strong type whose own converter rejects null) that check lives
            // on T's converter and will raise before we see it here. For reference T and
            // Nullable<T>, the caller asked for a collection whose element type admits
            // null, so admitting it is the correct behavior.
            var element = JsonSerializer.Deserialize<T>(ref reader, options);
            buffer.Add(element!);
        }

        throw new JsonException($"Unexpected end of JSON while reading {typeof(NonEmptyEnumerable<T>).Name}.");
    }

    private static void WriteCore<T>(Utf8JsonWriter writer, INonEmptyEnumerable<T>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        // Prefer the concrete type's span when we have it — avoids interface dispatch per element.
        if (value is NonEmptyEnumerable<T> concrete)
            foreach (var element in concrete.AsSpan())
                JsonSerializer.Serialize(writer, element, options);
        else
            for (var i = 0; i < value.Count; i++)
                JsonSerializer.Serialize(writer, value[i], options);
        writer.WriteEndArray();
    }

    private sealed class Inner<T> : JsonConverter<NonEmptyEnumerable<T>?>
    {
        // HandleNull so STJ hands us the JSON null token instead of short-circuiting
        // to default(NonEmptyEnumerable<T>?) — we want "null in, null out" semantics.
        public override bool HandleNull => true;

        public override NonEmptyEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadCore<T>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, NonEmptyEnumerable<T>? value, JsonSerializerOptions options)
            => WriteCore(writer, value, options);
    }

    private sealed class InterfaceInner<T> : JsonConverter<INonEmptyEnumerable<T>?>
    {
        public override bool HandleNull => true;

        public override INonEmptyEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadCore<T>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, INonEmptyEnumerable<T>? value, JsonSerializerOptions options)
            => WriteCore(writer, value, options);
    }
}
