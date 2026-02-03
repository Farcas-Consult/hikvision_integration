using System.Text.Json;
using System.Text.Json.Serialization;

namespace hikvision_integration.Models;

/// <summary>
/// Converts JSON values to string, accepting both Number and String tokens.
/// Use when the API may return a value as either type (e.g. turnstileId: 12345 or "TS-12345").
/// </summary>
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var i) ? i.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null or JsonTokenType.None => null,
            _ => reader.GetString()
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
