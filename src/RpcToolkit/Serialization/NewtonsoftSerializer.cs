#if NETSTANDARD2_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RpcToolkit.Serialization
{
    /// <summary>
    /// Newtonsoft.Json serializer for .NET Standard 2.0
    /// </summary>
    internal static class NewtonsoftSerializer
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public static string Serialize(object? value, bool safeMode)
        {
            if (value == null) return "null";

            value = RpcWireFormat.Prepare(value, item => safeMode ? ApplySafeMode(item) : item);

            return JsonConvert.SerializeObject(value, DefaultSettings);
        }

        public static T? Deserialize<T>(string json, bool safeMode)
        {
            if (!safeMode)
            {
                return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }

            var normalized = RemoveSafeModeForType(ParseToken(json), typeof(T));
            return normalized.ToObject<T>(JsonSerializer.Create(DefaultSettings));
        }

        public static object? Deserialize(string json, Type targetType, bool safeMode)
        {
            if (!safeMode)
            {
                return JsonConvert.DeserializeObject(json, targetType, DefaultSettings);
            }

            var normalized = RemoveSafeModeForType(ParseToken(json), targetType);
            return normalized.ToObject(targetType, JsonSerializer.Create(DefaultSettings));
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

                if (!request.IsNotification)
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

            if (value is JObject jobj)
            {
                var result = new Dictionary<string, object?>();
                foreach (var property in jobj.Properties())
                    result[property.Name] = ApplySafeMode(property.Value);
                return result;
            }

            if (value is JArray jarr)
            {
                var result = new List<object?>();
                foreach (var item in jarr)
                    result.Add(ApplySafeMode(item));
                return result;
            }

            if (value is JValue jvalue)
                return ApplySafeMode(jvalue.Value);

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

        internal static JToken ParseToken(string json)
        {
            // Newtonsoft accepts NaN/Infinity literals, unlike JSON. Reject their unquoted
            // spellings, but leave numeric overflow (e.g. 1e999) to envelope validation.
            var inString = false;
            for (var i = 0; i < json.Length; i++)
            {
                if (inString && json[i] == '\\')
                    i++;
                else if (json[i] == '"')
                    inString = !inString;
                else if (!inString && (json[i] == 'N' || json[i] == 'I'))
                    throw new JsonReaderException("Non-finite number literals are not valid JSON.");
            }

            using var reader = new JsonTextReader(new System.IO.StringReader(json))
            {
                DateParseHandling = DateParseHandling.None
            };
            var token = JToken.ReadFrom(reader);
            if (reader.Read())
                throw new JsonReaderException("Additional content after JSON value.");
            return token;
        }

        private static JToken RemoveSafeModeForType(JToken token, Type targetType)
        {
            if (token is JArray array && targetType != typeof(string))
            {
                var itemType = targetType.IsArray ? targetType.GetElementType() :
                    targetType.IsGenericType ? targetType.GetGenericArguments()[0] : null;
                if (itemType == typeof(RpcRequest) || itemType == typeof(RpcResponse))
                {
                    var items = new JArray();
                    foreach (var item in array)
                        items.Add(RemoveSafeModeForType(item, itemType));
                    return items;
                }
            }

            if (token is JObject obj &&
                (targetType == typeof(RpcRequest) || targetType == typeof(RpcResponse) || targetType == typeof(RpcError)))
            {
                var result = new JObject();
                foreach (var property in obj.Properties())
                {
                    var isPayload = (targetType == typeof(RpcRequest) && property.Name == "params") ||
                        (targetType == typeof(RpcResponse) && property.Name == "result") ||
                        (targetType == typeof(RpcError) && property.Name == "data");
                    result[property.Name] = isPayload ? RemoveSafeMode(property.Value) :
                        targetType == typeof(RpcResponse) && property.Name == "error"
                            ? RemoveSafeModeForType(property.Value, typeof(RpcError))
                            : property.Value.DeepClone();
                }
                return result;
            }

            return RemoveSafeMode(token);
        }

        private static JToken RemoveSafeMode(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                var value = token.Value<string>();
                if (value == null)
                {
                    return JValue.CreateNull();
                }

                if (value.StartsWith("S:", StringComparison.Ordinal))
                {
                    return new JValue(value.Substring(2));
                }

                if (value.StartsWith("D:", StringComparison.Ordinal))
                {
                    return new JValue(DateTimeOffset.Parse(value.Substring(2)));
                }

                if (value.EndsWith("n", StringComparison.Ordinal) &&
                    BigInteger.TryParse(value.Substring(0, value.Length - 1), out var bi))
                {
                    return new JValue(bi.ToString());
                }

                return new JValue(value);
            }

            if (token is JArray array)
            {
                var result = new JArray();
                foreach (var item in array)
                {
                    result.Add(RemoveSafeMode(item));
                }
                return result;
            }

            if (token is JObject obj)
            {
                var result = new JObject();
                foreach (var property in obj.Properties())
                {
                    result[property.Name] = RemoveSafeMode(property.Value);
                }
                return result;
            }

            return token.DeepClone();
        }

        private static bool IsPlainObject(object value)
        {
            var type = value.GetType();
            return type.IsClass && type != typeof(string);
        }

        private static string GetJsonPropertyName(PropertyInfo property)
        {
            var propertyName = property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName;
            if (!string.IsNullOrEmpty(propertyName))
                return propertyName!;

            return char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        }
    }
}

#endif
