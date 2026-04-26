using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary><see cref="JsonConverterFactory"/> for any wrapper marked with <see cref="NumericWrapperAttribute"/> — including the built-in <see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>, <see cref="Negative{T}"/>, <see cref="NonPositive{T}"/>, and <see cref="BoundedInt{TBounds}"/>, and any user-defined wrapper that follows the same shape.</summary>
/// <remarks>Reads and writes the underlying numeric value; JSON values that fail the wrapper's invariant throw <see cref="JsonException"/>.</remarks>
public sealed class NumericStrongTypeJsonConverterFactory : JsonConverterFactory
{
    // Inner<TWrapper, T> holds no mutable state and is safe to share across all
    // JsonSerializerOptions instances, so one cached converter per wrapper type
    // is plenty. Keyed by the closed wrapper type (e.g. Positive<int>).
    private static readonly ConcurrentDictionary<Type, JsonConverter> s_converterCache = new();

    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsValueType
        && typeToConvert.GetCustomAttribute<NumericWrapperAttribute>(inherit: false) is not null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        s_converterCache.GetOrAdd(typeToConvert, static t =>
        {
            // Read the underlying type from the wrapper's Value property rather
            // than the first generic argument: BoundedInt<TBounds> closes over
            // the bounds witness, so its first generic argument isn't the
            // underlying numeric type.
            var innerType = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!.PropertyType;
            var converterType = typeof(Inner<,>).MakeGenericType(t, innerType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });

    private sealed class Inner<TWrapper, T> : JsonConverter<TWrapper>
        where TWrapper : struct
    {
        private static readonly Func<TWrapper, T> s_getValue = BuildGetValue();
        private static readonly Func<T, TWrapper?> s_tryCreate = BuildTryCreate();

        private static Func<TWrapper, T> BuildGetValue()
        {
            var param = Expression.Parameter(typeof(TWrapper), "wrapper");
            var valueAccess = Expression.Property(param, "Value");
            return Expression.Lambda<Func<TWrapper, T>>(valueAccess, param).Compile();
        }

        private static Func<T, TWrapper?> BuildTryCreate()
        {
            var param = Expression.Parameter(typeof(T), "value");
            var method = typeof(TWrapper).GetMethod("TryCreate", BindingFlags.Public | BindingFlags.Static, [typeof(T)])!;
            var call = Expression.Call(method, param);
            return Expression.Lambda<Func<T, TWrapper?>>(call, param).Compile();
        }

        public override TWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize<T>(ref reader, options)!;
            return s_tryCreate(value)
                ?? throw new JsonException($"The JSON value '{value}' cannot be converted to {typeof(TWrapper).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, s_getValue(value), options);
        }
    }
}
