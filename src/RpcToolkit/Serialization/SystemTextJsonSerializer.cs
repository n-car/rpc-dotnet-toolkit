#if NET6_0_OR_GREATER

using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
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
            var obj = JsonSerializer.Deserialize<T>(json, DefaultOptions);

            if (safeMode && obj != null)
            {
                return (T?)RemoveSafeMode(obj);
            }

            return obj;
        }

        public static object? Deserialize(string json, Type targetType, bool safeMode)
        {
            var obj = JsonSerializer.Deserialize(json, targetType, DefaultOptions);

            if (safeMode && obj != null)
            {
                return RemoveSafeMode(obj);
            }

            return obj;
        }

        private static object? ApplySafeMode(object? value)
        {
            if (value == null) return null;

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

        private static object? RemoveSafeMode(object? value)
        {
            if (value == null) return null;

            // Handle safe-mode strings
            if (value is string str)
            {
                if (str.StartsWith("S:"))
                {
                    return str.Substring(2);
                }
                if (str.StartsWith("D:"))
                {
                    return DateTimeOffset.Parse(str.Substring(2));
                }
                if (str.EndsWith("n") && BigInteger.TryParse(str.Substring(0, str.Length - 1), out var bi))
                {
                    return bi;
                }
            }

            // Handle JsonElement
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => RemoveSafeMode(element.GetString()),
                    JsonValueKind.Array => RemoveSafeModeArray(element),
                    JsonValueKind.Object => RemoveSafeModeObject(element),
                    _ => value
                };
            }

            return value;
        }

        private static object RemoveSafeModeArray(JsonElement element)
        {
            var result = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                result.Add(RemoveSafeMode(item));
            }
            return result;
        }

        private static object RemoveSafeModeObject(JsonElement element)
        {
            var result = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = RemoveSafeMode(prop.Value);
            }
            return result;
        }
    }
}

#endif
