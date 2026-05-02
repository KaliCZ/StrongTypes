using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

public sealed class EmailJsonConverter : JsonConverter<Email>
{
    public override Email Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return Email.TryCreate(value)
            ?? throw new JsonException($"The JSON value '{value}' cannot be converted to {nameof(Email)}.");
    }

    public override void Write(Utf8JsonWriter writer, Email value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Address);
    }
}
