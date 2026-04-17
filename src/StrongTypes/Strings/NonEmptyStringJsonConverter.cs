#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

public class NonEmptyStringJsonConverter : JsonConverter<NonEmptyString?>
{
    public override NonEmptyString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // JSON null is the legitimate null case; let the caller decide whether
        // that's allowed by the target field's nullability.
        if (reader.TokenType == JsonTokenType.Null) return null;

        var value = reader.GetString();
        return NonEmptyString.TryCreate(value)
            ?? throw new JsonException($"The JSON value '{value}' cannot be converted to {nameof(NonEmptyString)}.");
    }

    public override void Write(Utf8JsonWriter writer, NonEmptyString? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
