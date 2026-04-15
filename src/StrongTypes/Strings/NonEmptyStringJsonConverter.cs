#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes;

public class NonEmptyStringJsonConverter : JsonConverter<NonEmptyString?>
{
    public override NonEmptyString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        NonEmptyString.TryCreate(reader.GetString());

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
