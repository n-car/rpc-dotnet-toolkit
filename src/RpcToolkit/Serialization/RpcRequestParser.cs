using System.Collections.Generic;
#if NETSTANDARD2_0
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace RpcToolkit.Serialization
{
    // Validate the wire envelope before typed deserialization can apply defaults or coerce values.
    internal static class RpcRequestParser
    {
        public static List<RpcRequest> Parse(string json, bool safeMode, out bool isBatch)
        {
            var requests = new List<RpcRequest>();
#if NETSTANDARD2_0
            var root = NewtonsoftSerializer.ParseToken(json);
            isBatch = root is JArray;
            if (root is JArray array)
            {
                foreach (var item in array)
                    requests.Add(ParseItem(item, safeMode));
            }
            else
                requests.Add(ParseItem(root, safeMode));
#else
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            isBatch = root.ValueKind == JsonValueKind.Array;
            if (isBatch)
            {
                foreach (var item in root.EnumerateArray())
                    requests.Add(ParseItem(item, safeMode));
            }
            else
                requests.Add(ParseItem(root, safeMode));
#endif
            return requests;
        }

#if NETSTANDARD2_0
        private static RpcRequest ParseItem(JToken item, bool safeMode)
        {
            if (item is not JObject obj ||
                obj["jsonrpc"]?.Type != JTokenType.String || obj["jsonrpc"]!.Value<string>() != "2.0" ||
                obj["method"]?.Type != JTokenType.String || string.IsNullOrEmpty(obj["method"]!.Value<string>()) ||
                (obj.TryGetValue("params", out var parameters) && parameters.Type != JTokenType.Object && parameters.Type != JTokenType.Array) ||
                (obj.TryGetValue("id", out var id) && id.Type != JTokenType.Null && id.Type != JTokenType.String &&
                    ((id.Type != JTokenType.Integer && id.Type != JTokenType.Float) ||
                     !double.TryParse(id.ToString(Formatting.None), System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out var number) ||
                     double.IsNaN(number) || double.IsInfinity(number))))
                return new RpcRequest { InvalidEnvelope = true };

            var request = new RpcRequest { Method = obj["method"]!.Value<string>()! };
            if (obj.TryGetValue("params", out parameters))
                request.Params = SerializerFactory.Deserialize<object>(parameters.ToString(Formatting.None), safeMode);
            if (obj.TryGetValue("id", out id))
                request.Id = id.Type == JTokenType.Null ? null : id.DeepClone();
            return request;
        }
#else
        private static RpcRequest ParseItem(JsonElement item, bool safeMode)
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("jsonrpc", out var version) || version.ValueKind != JsonValueKind.String || version.GetString() != "2.0" ||
                !item.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(method.GetString()) ||
                (item.TryGetProperty("params", out var parameters) && parameters.ValueKind != JsonValueKind.Object && parameters.ValueKind != JsonValueKind.Array) ||
                (item.TryGetProperty("id", out var id) && id.ValueKind != JsonValueKind.Null && id.ValueKind != JsonValueKind.String &&
                    (id.ValueKind != JsonValueKind.Number || !id.TryGetDouble(out var number) || double.IsNaN(number) || double.IsInfinity(number))))
                return new RpcRequest { InvalidEnvelope = true };

            var request = new RpcRequest { Method = method.GetString()! };
            if (item.TryGetProperty("params", out parameters))
                request.Params = SerializerFactory.Deserialize<object>(parameters.GetRawText(), safeMode);
            if (item.TryGetProperty("id", out id))
                request.Id = id.ValueKind == JsonValueKind.Null ? null : id.Clone();
            return request;
        }
#endif
    }
}
