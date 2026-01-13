using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErrorOr.Serialization;

/// <summary>
/// JSON converter for the <see cref="Error"/> type.
/// </summary>
public class ErrorJsonConverter : JsonConverter<Error>
{
    /// <inheritdoc/>
    public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }

        string code = "General.Unknown";
        string description = "An unknown error occurred.";
        int numericType = 0;
        Dictionary<string, object>? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name");
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName.ToLowerInvariant())
            {
                case "code":
                    code = reader.GetString() ?? code;
                    break;
                case "description":
                    description = reader.GetString() ?? description;
                    break;
                case "type":
                    // Type is serialized as a string (enum name), parse it
                    var typeString = reader.GetString();
                    if (typeString is not null && Enum.TryParse<ErrorType>(typeString, ignoreCase: true, out var errorType))
                    {
                        numericType = (int)errorType;
                    }

                    break;
                case "numerictype":
                    numericType = reader.GetInt32();
                    break;
                case "metadata":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                    }

                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return Error.Custom(numericType, code, description, metadata);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("code", value.Code);
        writer.WriteString("description", value.Description);
        writer.WriteString("type", value.Type.ToString());
        writer.WriteNumber("numericType", value.NumericType);

        if (value.Metadata is not null)
        {
            writer.WritePropertyName("metadata");
            JsonSerializer.Serialize(writer, value.Metadata, options);
        }

        writer.WriteEndObject();
    }
}
