using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErrorOr.Serialization;

/// <summary>
/// Factory for creating <see cref="ErrorOrJsonConverter{TValue}"/> instances.
/// </summary>
public class ErrorOrJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(ErrorOr<>);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ErrorOrJsonConverter<>).MakeGenericType(valueType);

        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
