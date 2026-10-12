using System.Text.Json;
using RpcToolkit.Serialization;

namespace RpcToolkit.Tests;

public class ProtocolCompatibilityTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("\"text\"")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"method\":\"echo\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"1.0\",\"method\":\"echo\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":2.0,\"method\":\"echo\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":1,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":null,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"params\":null,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"params\":false,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"params\":\"text\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"params\":42,\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":true}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":[]}")]
    public async Task InvalidEnvelope_AlwaysRespondsWithInvalidRequestAndNullId(string json)
    {
        var endpoint = NewEndpoint();
        using var response = JsonDocument.Parse(await endpoint.HandleRequestAsync(json));
        AssertError(response.RootElement, -32600);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("id").ValueKind);
    }

    [Theory]
    [InlineData(false, "{\"jsonrpc\":\"2.0\",\"method\":\"\",\"id\":1}")]
    [InlineData(true, "{\"jsonrpc\":\"2.0\",\"method\":\"\",\"id\":1}")]
    [InlineData(false, "{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":1e999}")]
    [InlineData(true, "{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":1e999}")]
    public async Task EmptyMethodAndNonFiniteNumericId_AreInvalidEnvelopes(bool safeMode, string json)
    {
        using var response = JsonDocument.Parse(await NewEndpoint(safeMode).HandleRequestAsync(json));
        AssertError(response.RootElement, -32600);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("id").ValueKind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhitespaceMethod_IsAnUnknownMethodRatherThanAnInvalidEnvelope(bool safeMode)
    {
        using var response = JsonDocument.Parse(await NewEndpoint(safeMode).HandleRequestAsync(
            "{\"jsonrpc\":\"2.0\",\"method\":\" \",\"id\":1}"));
        AssertError(response.RootElement, -32601);
        Assert.Equal(1, response.RootElement.GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitNullId_ReturnsRequiredNullResultAndId(bool safeMode)
    {
        var endpoint = NewEndpoint(safeMode);
        endpoint.AddMethod<object, object?>("empty", (_, _) => (object?)null);
        using var response = JsonDocument.Parse(await endpoint.HandleRequestAsync(
            "{\"jsonrpc\":\"2.0\",\"method\":\"empty\",\"id\":null}"));
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("id").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("result").ValueKind);
        Assert.False(response.RootElement.TryGetProperty("error", out _));
        Assert.False(response.RootElement.TryGetProperty("hasError", out _));
    }

    [Theory]
    [InlineData("{not-json")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"id\":NaN}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"echo\",\"params\":[Infinity],\"id\":1}")]
    [InlineData("{} {}")]
    public async Task MalformedJson_IsParseError(string json)
    {
        using var response = JsonDocument.Parse(await NewEndpoint().HandleRequestAsync(json));
        AssertError(response.RootElement, -32700);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("id").ValueKind);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MixedBatch_ProcessesInvalidItemsAndNotificationsIndependently(bool parallel, bool safeMode)
    {
        var endpoint = NewEndpoint(safeMode, parallel);
        var invoked = 0;
        endpoint.AddMethod<object, int>("count", (_, _) => Interlocked.Increment(ref invoked));
        using var response = JsonDocument.Parse(await endpoint.HandleRequestAsync(
            "[null,7,{},[],{\"jsonrpc\":\"2.0\",\"method\":\"count\"}," +
            "{\"jsonrpc\":\"2.0\",\"method\":\"count\",\"id\":null}," +
            "{\"jsonrpc\":\"2.0\",\"method\":\"count\",\"id\":5}]"));
        var items = response.RootElement.EnumerateArray().ToArray();
        Assert.Equal(6, items.Length);
        Assert.Equal(3, invoked);
        foreach (var item in items.Take(4))
        {
            AssertError(item, -32600);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("id").ValueKind);
        }
        Assert.Equal(JsonValueKind.Null, items[4].GetProperty("id").ValueKind);
        Assert.Equal(5, items[5].GetProperty("id").GetInt32());
        Assert.Empty(await endpoint.HandleRequestAsync(
            "[{\"jsonrpc\":\"2.0\",\"method\":\"count\"},{\"jsonrpc\":\"2.0\",\"method\":\"missing\"}]"));
    }

    [Theory]
    [InlineData("S:echo", "S:request")]
    [InlineData("D:not-a-date", "D:not-a-date")]
    [InlineData("123n", "123n")]
    [InlineData("S:echo", "2026-01-02T03:04:05Z")]
    public async Task SafeMode_DoesNotDecodeMethodOrId(string method, string id)
    {
        var endpoint = NewEndpoint(true);
        endpoint.AddMethod<Dictionary<string, string>, string>(method, (parameters, _) => parameters!["text"]);
        var wireRequest = JsonSerializer.Serialize(new { jsonrpc = "2.0", method, id, @params = new { text = "S:hello" } });
        using var response = JsonDocument.Parse(await endpoint.HandleRequestAsync(wireRequest));
        Assert.Equal(id, response.RootElement.GetProperty("id").GetString());
        Assert.Equal("S:hello", response.RootElement.GetProperty("result").GetString());
    }

    [Fact]
    public void SafeMode_DecodesOnlyErrorDataInSingleAndBatchResponses()
    {
        const string json = "{\"jsonrpc\":\"2.0\",\"id\":\"123n\",\"error\":{\"code\":42,\"message\":\"D:not-a-date\",\"data\":{\"method\":\"S:hello\"}}}";
        var response = SerializerFactory.Deserialize<RpcResponse>(json, true)!;
        Assert.Equal("123n", response.Id!.ToString());
        Assert.Equal("D:not-a-date", response.Error!.Message);
        using var data = JsonDocument.Parse(SerializerFactory.Serialize(response.Error.Data));
        Assert.Equal("hello", data.RootElement.GetProperty("method").GetString());
        var batch = SerializerFactory.Deserialize<List<RpcResponse>>("[" + json + "]", true)!;
        Assert.Equal("123n", batch[0].Id!.ToString());
        Assert.Equal("D:not-a-date", batch[0].Error!.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequestSerialization_PreservesNotificationAndExplicitNullId(bool safeMode)
    {
        var notification = new RpcRequest("notify");
        Assert.True(notification.IsNotification);
        using var notificationJson = JsonDocument.Parse(SerializerFactory.Serialize(notification, safeMode));
        Assert.False(notificationJson.RootElement.TryGetProperty("id", out _));
        Assert.False(notificationJson.RootElement.TryGetProperty("isNotification", out _));

        var request = new RpcRequest("call") { Id = null };
        Assert.False(request.IsNotification);
        using var requestJson = JsonDocument.Parse(SerializerFactory.Serialize(request, safeMode));
        Assert.Equal(JsonValueKind.Null, requestJson.RootElement.GetProperty("id").ValueKind);
        var deserialized = SerializerFactory.Deserialize<RpcRequest>(requestJson.RootElement.GetRawText(), safeMode)!;
        Assert.False(deserialized.IsNotification);
    }

    private static RpcEndpoint NewEndpoint(bool safeMode = false, bool parallel = false) => new(options: new RpcOptions
    {
        SafeEnabled = safeMode,
        EnableLogging = false,
        BatchOptions = new RpcBatchOptions { Parallel = parallel }
    });

    private static void AssertError(JsonElement response, int code)
    {
        Assert.Equal("2.0", response.GetProperty("jsonrpc").GetString());
        Assert.Equal(code, response.GetProperty("error").GetProperty("code").GetInt32());
        Assert.False(response.TryGetProperty("result", out _));
    }
}
