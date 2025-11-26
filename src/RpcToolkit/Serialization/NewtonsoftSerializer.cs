#if NETSTANDARD2_0

using System;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RpcToolkit.Serialization
{
    /// <summary>
    /// Newtonsoft.Json serializer for .NET Standard 2.0
    /// </summary>
    internal static class NewtonsoftSerializer
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public static string Serialize(object? value, bool safeMode)
        {
            if (value == null) return "null";

            if (safeMode)
            {
                value = ApplySafeMode(value);
            }

            return JsonConvert.SerializeObject(value, DefaultSettings);
        }

        public static T? Deserialize<T>(string json, bool safeMode)
        {
            var obj = JsonConvert.DeserializeObject<T>(json, DefaultSettings);

            if (safeMode && obj != null)
            {
                return (T?)RemoveSafeMode(obj);
            }

            return obj;
        }

        public static object? Deserialize(string json, Type targetType, bool safeMode)
        {
            var obj = JsonConvert.DeserializeObject(json, targetType, DefaultSettings);

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

            // Handle JObject (nested objects)
            if (value is JObject jobj)
            {
                var result = new JObject();
                foreach (var prop in jobj.Properties())
                {
                    result[prop.Name] = JToken.FromObject(ApplySafeMode(prop.Value.ToObject<object>()) ?? new object());
                }
                return result;
            }

            return value;
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

            // Handle arrays
            if (value is JArray jarr)
            {
                var result = new object?[jarr.Count];
                for (int i = 0; i < jarr.Count; i++)
                {
                    result[i] = RemoveSafeMode(jarr[i].ToObject<object>());
                }
                return result;
            }

            // Handle objects
            if (value is JObject jobj)
            {
                var result = new JObject();
                foreach (var prop in jobj.Properties())
                {
                    result[prop.Name] = JToken.FromObject(RemoveSafeMode(prop.Value.ToObject<object>()) ?? new object());
                }
                return result;
            }

            return value;
        }
    }
}

#endif
