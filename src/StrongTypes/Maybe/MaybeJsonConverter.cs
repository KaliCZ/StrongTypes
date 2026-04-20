#nullable enable

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// <see cref="JsonConverterFactory"/> for <see cref="Maybe{T}"/> and for
/// <see cref="Nullable{T}"/> of <see cref="Maybe{T}"/>. Both use the flat wire
/// format — the underlying <typeparamref name="T"/>'s JSON value, not an object
/// wrapper:
/// <list type="bullet">
///   <item><description><c>Maybe&lt;T&gt;</c> serializes <c>Some(x)</c> as the raw
///     JSON representation of <c>x</c> and <c>None</c> as <c>null</c>.
///     Deserialization is the inverse: <c>null</c> ⇒ <c>None</c>, any value ⇒
///     <c>Some</c>.</description></item>
///   <item><description><c>Maybe&lt;T&gt;?</c> distinguishes three states on the
///     wire — the three states of an HTTP <c>PATCH</c> body:
///     <list type="bullet">
///       <item><description>property absent ⇒ the property stays <c>null</c> (STJ
///         never invokes the converter).</description></item>
///       <item><description>property <c>null</c> ⇒ the property is a present
///         nullable whose value is <c>Maybe&lt;T&gt;.None</c>.</description></item>
///       <item><description>property has a value ⇒ the property is a present
///         nullable whose value is <c>Maybe&lt;T&gt;.Some(x)</c>.</description></item>
///     </list>
///     This only works when the factory is registered in
///     <see cref="JsonSerializerOptions.Converters"/> — the <c>[JsonConverter]</c>
///     attribute on <see cref="Maybe{T}"/> is resolved for <c>Maybe&lt;T&gt;</c>
///     only, and STJ's built-in <c>Nullable&lt;T&gt;</c> handling would otherwise
///     collapse JSON <c>null</c> into a <c>null</c> nullable and lose the
///     distinction from absence.</description></item>
/// </list>
/// </summary>
public sealed class MaybeJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        var generic = typeToConvert.GetGenericTypeDefinition();
        if (generic == typeof(Maybe<>)) return true;
        if (generic == typeof(Nullable<>))
        {
            var inner = typeToConvert.GetGenericArguments()[0];
            return inner.IsGenericType && inner.GetGenericTypeDefinition() == typeof(Maybe<>);
        }
        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            if (t.GetGenericTypeDefinition() == typeof(Maybe<>))
            {
                var innerType = t.GetGenericArguments()[0];
                var converterType = typeof(Inner<>).MakeGenericType(innerType);
                return (JsonConverter)Activator.CreateInstance(converterType)!;
            }

            // Nullable<Maybe<T>> — unwrap twice to get T.
            var maybeType = t.GetGenericArguments()[0];
            var valueType = maybeType.GetGenericArguments()[0];
            var nullableConverterType = typeof(NullableInner<>).MakeGenericType(valueType);
            return (JsonConverter)Activator.CreateInstance(nullableConverterType)!;
        });

    private sealed class Inner<T> : JsonConverter<Maybe<T>> where T : notnull
    {
        public override bool HandleNull => true;

        public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return default;
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return value is null ? default : Maybe<T>.Some(value);
        }

        public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                JsonSerializer.Serialize(writer, value.InternalValue, options);
            else
                writer.WriteNullValue();
        }
    }

    private sealed class NullableInner<T> : JsonConverter<Maybe<T>?> where T : notnull
    {
        public override bool HandleNull => true;

        public override Maybe<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return Maybe<T>.None;
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return value is null ? Maybe<T>.None : Maybe<T>.Some(value);
        }

        public override void Write(Utf8JsonWriter writer, Maybe<T>? value, JsonSerializerOptions options)
        {
            if (value is { } m && m.HasValue)
                JsonSerializer.Serialize(writer, m.InternalValue, options);
            else
                writer.WriteNullValue();
        }
    }
}
