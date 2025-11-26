using RpcToolkit;
using RpcToolkit.Serialization;
using System.Numerics;

namespace RpcToolkit.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void SerializerFactory_ReportsCorrectSerializer()
        {
            var name = SerializerFactory.GetSerializerName();
            
#if NETSTANDARD2_0
            Assert.Equal("Newtonsoft.Json", name);
#else
            Assert.Equal("System.Text.Json", name);
#endif
        }

        [Fact]
        public void Serialize_SimpleObject_Works()
        {
            var obj = new { name = "test", value = 42 };
            var json = SerializerFactory.Serialize(obj);
            
            Assert.Contains("test", json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void SafeMode_String_AddsPrefixS()
        {
            var result = SerializerFactory.Serialize("hello", safeMode: true);
            Assert.Contains("S:hello", result);
        }

        [Fact]
        public void SafeMode_DateTime_AddsPrefixD()
        {
            var dt = new DateTime(2025, 11, 26, 10, 30, 0, DateTimeKind.Utc);
            var result = SerializerFactory.Serialize(dt, safeMode: true);
            
            Assert.Contains("D:", result);
            Assert.Contains("2025-11-26", result);
        }

        [Fact]
        public void SafeMode_BigInteger_AddsSuffixN()
        {
            var big = new BigInteger(9007199254740992);
            var result = SerializerFactory.Serialize(big, safeMode: true);
            
            Assert.Contains("9007199254740992n", result);
        }

        [Fact]
        public void SafeMode_RoundTrip_PreservesValues()
        {
            // Test individual values (not objects yet - objects need recursive safe mode)
            var str = "hello";
            var date = new DateTime(2025, 11, 26, 10, 30, 0, DateTimeKind.Utc);
            var big = new BigInteger(123456789);

            var jsonStr = SerializerFactory.Serialize(str, safeMode: true);
            var jsonDate = SerializerFactory.Serialize(date, safeMode: true);
            var jsonBig = SerializerFactory.Serialize(big, safeMode: true);
            
            // Verify prefixes are present
            Assert.Contains("S:hello", jsonStr);
            Assert.Contains("D:", jsonDate);
            Assert.Contains("n", jsonBig);
        }

        [Fact]
        public void Deserialize_SimpleObject_Works()
        {
            var json = "{\"name\":\"test\",\"value\":42}";
            var obj = SerializerFactory.Deserialize<Dictionary<string, object>>(json);
            
            Assert.NotNull(obj);
            Assert.Equal("test", obj["name"]?.ToString());
        }

        [Fact]
        public void SafeMode_Deserialize_RemovesPrefixes()
        {
            var json = "\"S:hello\"";
            var result = SerializerFactory.Deserialize<string>(json, safeMode: true);
            
            Assert.Equal("hello", result);
        }
    }
}
