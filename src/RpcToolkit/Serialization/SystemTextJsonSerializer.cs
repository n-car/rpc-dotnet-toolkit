#if NET6_0_OR_GREATER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RpcToolkit.Serialization
{
    /// <summary>
    /// System.Text.Json serializer for .NET 6+
    /// </summary>
    internal static class SystemTextJsonSerializer
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new BigIntegerJsonConverter() }
        };

        public static string Serialize(object? value, bool safeMode)
        {
            if (value == null) return "null";

            if (safeMode)
            {
                value = ApplySafeMode(value);
            }

            return JsonSerializer.Serialize(value, DefaultOptions);
        }

        public static T? Deserialize<T>(string json, bool safeMode)
        {
            if (!safeMode)
            {
                return JsonSerializer.Deserialize<T>(json, DefaultOptions);
            }

            using var document = JsonDocument.Parse(json);
            var normalized = RemoveSafeMode(document.RootElement);
            var normalizedJson = JsonSerializer.Serialize(normalized, DefaultOptions);
            return JsonSerializer.Deserialize<T>(normalizedJson, DefaultOptions);
        }

        public static object? Deserialize(string json, Type targetType, bool safeMode)
        {
            if (!safeMode)
            {
                return JsonSerializer.Deserialize(json, targetType, DefaultOptions);
            }

            using var document = JsonDocument.Parse(json);
            var normalized = RemoveSafeMode(document.RootElement);
            var normalizedJson = JsonSerializer.Serialize(normalized, DefaultOptions);
            return JsonSerializer.Deserialize(normalizedJson, targetType, DefaultOptions);
        }

        private static object? ApplySafeMode(object? value)
        {
            if (value == null) return null;

            if (value is RpcRequest request)
            {
                var result = new Dictionary<string, object?>
                {
                    ["jsonrpc"] = request.JsonRpc,
                    ["method"] = request.Method
                };

                if (request.Params != null)
                {
                    result["params"] = ApplySafeMode(request.Params);
                }

                if (request.Id != null)
                {
                    result["id"] = request.Id;
                }

                return result;
            }

            if (value is RpcResponse response)
            {
                var result = new Dictionary<string, object?>
                {
                    ["jsonrpc"] = response.JsonRpc,
                    ["id"] = response.Id
                };

                if (response.Error != null)
                {
                    result["error"] = ApplySafeMode(response.Error);
                }
                else
                {
                    result["result"] = ApplySafeMode(response.Result);
                }

                return result;
            }

            if (value is RpcError error)
            {
                var result = new Dictionary<string, object?>
                {
                    ["code"] = error.Code,
                    ["message"] = error.Message
                };

                if (error.Data != null)
                {
                    result["data"] = ApplySafeMode(error.Data);
                }

                return result;
            }

            // Handle strings: prefix with "S:"
            if (value is string str)
            {
                return "S:" + str;
            }

            // Handle DateTime/DateTimeOffset: prefix with "D:"
            if (value is DateTime dt)
            {
                return "D:" + dt.ToUniversalTime().ToString("o");
            }
            if (value is DateTimeOffset dto)
            {
                return "D:" + dto.ToUniversalTime().ToString("o");
            }

            // Handle BigInteger: append "n"
            if (value is BigInteger bi)
            {
                return bi.ToString() + "n";
            }

            // Handle JsonElement (for dynamic parsing)
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => "S:" + element.GetString(),
                    JsonValueKind.Array => ApplySafeModeArray(element),
                    JsonValueKind.Object => ApplySafeModeObject(element),
                    _ => value
                };
            }

            if (value is IDictionary dictionary)
            {
                var result = new Dictionary<string, object?>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key == null)
                        continue;

                    result[entry.Key.ToString()!] = ApplySafeMode(entry.Value);
                }
                return result;
            }

            // Handle arrays
            if (value is Array arr)
            {
                var result = new object?[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    result[i] = ApplySafeMode(arr.GetValue(i));
                }
                return result;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var result = new List<object?>();
                foreach (var item in enumerable)
                {
                    result.Add(ApplySafeMode(item));
                }
                return result;
            }

            if (IsPlainObject(value))
            {
                var result = new Dictionary<string, object?>();
                foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length > 0)
                        continue;

                    var propertyValue = property.GetValue(value);
                    if (propertyValue == null)
                        continue;

                    result[GetJsonPropertyName(property)] = ApplySafeMode(propertyValue);
                }
                return result;
            }

            return value;
        }

        private static object ApplySafeModeArray(JsonElement element)
        {
            var result = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                result.Add(ApplySafeMode(item));
            }
            return result;
        }

        private static object ApplySafeModeObject(JsonElement element)
        {
            var result = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = ApplySafeMode(prop.Value);
            }
            return result;
        }

        private static object? RemoveSafeMode(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var value = element.GetString();
                    if (value == null)
                        return null;

                    if (value.StartsWith("S:", StringComparison.Ordinal))
                    {
                        return value.Substring(2);
                    }

                    if (value.StartsWith("D:", StringComparison.Ordinal))
                    {
                        return DateTimeOffset.Parse(value.Substring(2));
                    }

                    if (value.EndsWith("n", StringComparison.Ordinal) &&
                        BigInteger.TryParse(value.Substring(0, value.Length - 1), out var bi))
                    {
                        return bi;
                    }

                    return value;
                }
                case JsonValueKind.Array:
                    return RemoveSafeModeArray(element);
                case JsonValueKind.Object:
                    return RemoveSafeModeObject(element);
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    if (element.TryGetDecimal(out var decimalValue))
                        return decimalValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return null;
            }
        }

        private static List<object?> RemoveSafeModeArray(JsonElement element)
        {
            var result = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                result.Add(RemoveSafeMode(item));
            }
            return result;
        }

        private static Dictionary<string, object?> RemoveSafeModeObject(JsonElement element)
        {
            var result = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = RemoveSafeMode(prop.Value);
            }
            return result;
        }

        private static bool IsPlainObject(object value)
        {
            var type = value.GetType();
            return type.IsClass && type != typeof(string);
        }

        private static string GetJsonPropertyName(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            return attribute?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        }

        private sealed class BigIntegerJsonConverter : JsonConverter<BigInteger>
        {
            public override BigInteger Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString() ?? "0";
                    if (value.EndsWith("n", StringComparison.Ordinal))
                    {
                        value = value.Substring(0, value.Length - 1);
                    }
                    return BigInteger.Parse(value, CultureInfo.InvariantCulture);
                }

                if (reader.TokenType == JsonTokenType.Number &&
                    reader.TryGetInt64(out var longValue))
                {
                    return new BigInteger(longValue);
                }

                throw new JsonException("Expected JSON string or integer number for BigInteger.");
            }

            public override void Write(
                Utf8JsonWriter writer,
                BigInteger value,
                JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}

#endif
