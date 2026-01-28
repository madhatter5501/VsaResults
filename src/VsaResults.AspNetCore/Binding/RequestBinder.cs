using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace VsaResults.Binding;

/// <summary>
/// Manual request binder that provides full control over model binding,
/// allowing wide events to be emitted for binding failures.
/// </summary>
public static class RequestBinder
{
    private static readonly ConcurrentDictionary<Type, PropertyBindingInfo[]> BindingInfoCache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Binds a request from the HTTP context.
    /// </summary>
    /// <typeparam name="TRequest">The request type to bind.</typeparam>
    /// <param name="context">The HTTP context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bound request or binding errors.</returns>
    public static async ValueTask<VsaResult<TRequest>> BindAsync<TRequest>(
        HttpContext context,
        CancellationToken ct = default)
        where TRequest : new()
    {
        var request = new TRequest();
        var errors = new List<Error>();
        var bindingInfos = GetBindingInfos<TRequest>();

        // Pre-read body if any properties need it
        var bodyState = await ReadBodyIfNeededAsync(context, bindingInfos, ct);

        foreach (var info in bindingInfos)
        {
            try
            {
                object? value = info.Source switch
                {
                    BindingSource.Route => BindFromRoute(context, info),
                    BindingSource.Query => BindFromQuery(context, info),
                    BindingSource.Header => BindFromHeader(context, info),
                    BindingSource.Body => BindFromBody(info, bodyState),
                    BindingSource.Services => BindFromServices(context, info),
                    _ => GetDefaultValue(info),
                };

                // Check for required properties
                if (value is null && info.IsRequired)
                {
                    errors.Add(Error.Validation(
                        $"Binding.{info.PropertyName}.Required",
                        $"The {info.SourceName} parameter '{info.BindingName}' is required."));
                    continue;
                }

                // Convert value if needed
                if (value is not null && value.GetType() != info.PropertyType)
                {
                    var convertResult = ConvertValue(value, info.PropertyType, info.BindingName);
                    if (convertResult.IsError)
                    {
                        errors.AddRange(convertResult.Errors);
                        continue;
                    }

                    value = convertResult.Value;
                }

                info.Property.SetValue(request, value);
            }
            catch (Exception ex)
            {
                errors.Add(Error.Validation(
                    $"Binding.{info.PropertyName}.Failed",
                    $"Failed to bind '{info.BindingName}' from {info.SourceName}: {ex.Message}"));
            }
        }

        return errors.Count > 0
            ? errors
            : request;
    }

    /// <summary>
    /// Gets binding context information for debugging/logging.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A dictionary of binding context for wide events.</returns>
    public static Dictionary<string, object?> GetBindingContext<TRequest>(HttpContext context)
    {
        var ctx = new Dictionary<string, object?>
        {
            ["request_type"] = typeof(TRequest).Name,
            ["http_method"] = context.Request.Method,
            ["http_path"] = context.Request.Path.Value,
        };

        // Add route values
        foreach (var (key, value) in context.Request.RouteValues)
        {
            ctx[$"route_{key}"] = value?.ToString();
        }

        // Add query parameters (limited to avoid bloat)
        var queryCount = 0;
        foreach (var (key, value) in context.Request.Query)
        {
            if (queryCount++ >= 10)
            {
                break;
            }

            ctx[$"query_{key}"] = value.ToString();
        }

        return ctx;
    }

    private static PropertyBindingInfo[] GetBindingInfos<TRequest>()
    {
        return BindingInfoCache.GetOrAdd(typeof(TRequest), type =>
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => new PropertyBindingInfo(p))
                .ToArray();
            return properties;
        });
    }

    private static async ValueTask<BodyState> ReadBodyIfNeededAsync(
        HttpContext context,
        PropertyBindingInfo[] bindingInfos,
        CancellationToken ct)
    {
        // Check if any property needs body binding
        var needsBody = bindingInfos.Any(i => i.Source == BindingSource.Body);
        if (!needsBody)
        {
            return new BodyState();
        }

        // Try to read and parse the body
        if (context.Request.ContentLength > 0 || context.Request.ContentType?.Contains("json") == true)
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct);
                return new BodyState { Json = doc.RootElement.Clone() };
            }
            catch
            {
                // Body is not valid JSON - will be handled as null
            }
        }

        return new BodyState();
    }

    private static object? BindFromRoute(HttpContext context, PropertyBindingInfo info)
    {
        if (context.Request.RouteValues.TryGetValue(info.BindingName, out var value))
        {
            return value;
        }

        return null;
    }

    private static object? BindFromQuery(HttpContext context, PropertyBindingInfo info)
    {
        if (context.Request.Query.TryGetValue(info.BindingName, out var values))
        {
            // Handle collections
            if (info.IsCollection)
            {
                return values.ToArray();
            }

            return values.Count > 0 ? values[0] : null;
        }

        return null;
    }

    private static object? BindFromHeader(HttpContext context, PropertyBindingInfo info)
    {
        if (context.Request.Headers.TryGetValue(info.BindingName, out var values))
        {
            return values.Count > 0 ? values[0] : null;
        }

        return null;
    }

    private static object? BindFromBody(PropertyBindingInfo info, BodyState bodyState)
    {
        if (bodyState.Json is not { } bodyJson)
        {
            return null;
        }

        // If the property is the entire body
        if (info.IsBodyRoot)
        {
            return JsonSerializer.Deserialize(bodyJson.GetRawText(), info.PropertyType, JsonOptions);
        }

        // Otherwise, look for a specific property in the body
        if (bodyJson.TryGetProperty(info.BindingName, out var prop) ||
            bodyJson.TryGetProperty(ToCamelCase(info.BindingName), out prop))
        {
            return JsonSerializer.Deserialize(prop.GetRawText(), info.PropertyType, JsonOptions);
        }

        return null;
    }

    private static object? BindFromServices(HttpContext context, PropertyBindingInfo info)
    {
        return context.RequestServices.GetService(info.PropertyType);
    }

    private static object? GetDefaultValue(PropertyBindingInfo info)
    {
        if (info.HasDefaultValue)
        {
            return info.DefaultValue;
        }

        return info.PropertyType.IsValueType
            ? Activator.CreateInstance(info.PropertyType)
            : null;
    }

    private static VsaResult<object?> ConvertValue(object value, Type targetType, string parameterName)
    {
        try
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle string to various types
            if (value is string stringValue)
            {
                // Handle Guid
                if (underlyingType == typeof(Guid))
                {
                    if (Guid.TryParse(stringValue, out var guid))
                    {
                        return guid;
                    }

                    return Error.Validation(
                        $"Binding.{parameterName}.InvalidGuid",
                        $"The value '{stringValue}' is not a valid GUID.");
                }

                // Handle enums
                if (underlyingType.IsEnum)
                {
                    if (Enum.TryParse(underlyingType, stringValue, ignoreCase: true, out var enumValue))
                    {
                        return enumValue;
                    }

                    var validValues = string.Join(", ", Enum.GetNames(underlyingType));
                    return Error.Validation(
                        $"Binding.{parameterName}.InvalidEnum",
                        $"The value '{stringValue}' is not valid. Valid values: {validValues}");
                }

                // Use TypeConverter for other types
                var converter = TypeDescriptor.GetConverter(underlyingType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFromString(stringValue);
                }
            }

            // Handle StringValues
            if (value is StringValues stringValues)
            {
                return ConvertValue(stringValues.ToString(), targetType, parameterName);
            }

            // Direct assignment if compatible
            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            // Try Convert.ChangeType
            return Convert.ChangeType(value, underlyingType);
        }
        catch (Exception ex)
        {
            return Error.Validation(
                $"Binding.{parameterName}.ConversionFailed",
                $"Failed to convert value to {targetType.Name}: {ex.Message}");
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// State container for parsed request body.
    /// </summary>
    private sealed class BodyState
    {
        public JsonElement? Json { get; init; }
    }
}
