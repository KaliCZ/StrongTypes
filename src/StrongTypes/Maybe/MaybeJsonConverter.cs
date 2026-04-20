#nullable enable

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// <see cref="JsonConverterFactory"/> for <see cref="Maybe{T}"/> that accepts any of
/// the following JSON shapes as inputs:
/// <list type="bullet">
///   <item><description><c>{}</c> — empty Maybe</description></item>
///   <item><description><c>{ "Value": null }</c> — empty Maybe</description></item>
///   <item><description><c>{ "Value": x }</c> — <see cref="Maybe{T}.Some"/> with the parsed value</description></item>
/// </list>
/// Writes an empty Maybe as <c>{ "Value": null }</c> and a populated Maybe as
/// <c>{ "Value": x }</c>.
/// </summary>
public sealed class MaybeJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Maybe<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            var innerType = t.GetGenericArguments()[0];
            var converterType = typeof(Inner<>).MakeGenericType(innerType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });

    private sealed class Inner<T> : JsonConverter<Maybe<T>> where T : notnull
    {
        public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected StartObject for {typeof(Maybe<T>).Name}, got {reader.TokenType}.");

            Maybe<T> result = default;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return result;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propName = reader.GetString();
                reader.Read();

                if (string.Equals(propName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        result = default;
                    }
                    else
                    {
                        var value = JsonSerializer.Deserialize<T>(ref reader, options);
                        // T : notnull at declaration, but the wire is honest — only
                        // wrap in Some when the parsed value is actually non-null.
                        if (value is not null)
                            result = Maybe<T>.Some(value);
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException($"Unexpected end of JSON while reading {typeof(Maybe<T>).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            if (value.HasValue)
                JsonSerializer.Serialize(writer, value.InternalValue, options);
            else
                writer.WriteNullValue();
            writer.WriteEndObject();
        }
    }
}
