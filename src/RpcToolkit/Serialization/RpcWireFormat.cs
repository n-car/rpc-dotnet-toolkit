using System;
using System.Collections;
using System.Collections.Generic;

namespace RpcToolkit.Serialization
{
    internal static class RpcWireFormat
    {
        // Required null envelope fields must survive serializers that omit null object properties.
        public static object? Prepare(object? value, Func<object?, object?> payload)
        {
            if (value is RpcRequest request)
            {
                var result = new Dictionary<string, object?>
                {
                    ["jsonrpc"] = request.JsonRpc,
                    ["method"] = request.Method
                };
                if (request.Params != null)
                    result["params"] = payload(request.Params);
                if (!request.IsNotification)
                    result["id"] = request.Id;
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
                    result["error"] = Prepare(response.Error, payload);
                else
                    result["result"] = payload(response.Result);
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
                    result["data"] = payload(error.Data);
                return result;
            }

            if ((value is IEnumerable<RpcRequest> || value is IEnumerable<RpcResponse>) && value is IEnumerable enumerable)
            {
                var items = new List<object?>();
                foreach (var item in enumerable)
                    items.Add(Prepare(item, payload));
                return items;
            }

            return payload(value);
        }
    }
}
