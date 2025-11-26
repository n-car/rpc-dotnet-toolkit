using System;

namespace RpcToolkit.Serialization
{
    /// <summary>
    /// Factory for creating the appropriate serializer based on target framework
    /// </summary>
    public static class SerializerFactory
    {
        /// <summary>
        /// Serialize an object to JSON string
        /// </summary>
        public static string Serialize(object? value, bool safeMode = false)
        {
#if NETSTANDARD2_0
            return NewtonsoftSerializer.Serialize(value, safeMode);
#else
            return SystemTextJsonSerializer.Serialize(value, safeMode);
#endif
        }

        /// <summary>
        /// Deserialize a JSON string to an object
        /// </summary>
        public static T? Deserialize<T>(string json, bool safeMode = false)
        {
#if NETSTANDARD2_0
            return NewtonsoftSerializer.Deserialize<T>(json, safeMode);
#else
            return SystemTextJsonSerializer.Deserialize<T>(json, safeMode);
#endif
        }

        /// <summary>
        /// Deserialize to a dynamic object
        /// </summary>
        public static object? Deserialize(string json, Type targetType, bool safeMode = false)
        {
#if NETSTANDARD2_0
            return NewtonsoftSerializer.Deserialize(json, targetType, safeMode);
#else
            return SystemTextJsonSerializer.Deserialize(json, targetType, safeMode);
#endif
        }

        /// <summary>
        /// Get the name of the serializer being used
        /// </summary>
        public static string GetSerializerName()
        {
#if NETSTANDARD2_0
            return "Newtonsoft.Json";
#else
            return "System.Text.Json";
#endif
        }
    }
}
