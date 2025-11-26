using System;
using System.Threading.Tasks;
using RpcToolkit;

namespace RpcToolkit.Examples
{
    /// <summary>
    /// Example: Using RpcSafeEndpoint and RpcSafeClient
    /// 
    /// This example demonstrates how to use the Safe mode classes
    /// for better type preservation and automatic safe mode handling.
    /// </summary>
    class SafeModeExample
    {
        static async Task Main(string[] args)
        {
            // ===== SERVER SIDE =====
            
            // Create a safe endpoint (Safe Mode enabled by default)
            var rpc = new RpcSafeEndpoint(new { Server = "example" });
            
            // Register methods that handle special types
            rpc.AddMethod<object, object>("getCurrentTime", (p, ctx) =>
            {
                return new
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    DateTime = DateTime.UtcNow,
                    TimeZone = TimeZoneInfo.Local.DisplayName
                };
            });
            
            rpc.AddMethod<MathParams, object>("mathOperations", (p, ctx) =>
            {
                return new
                {
                    Infinity = double.PositiveInfinity,
                    NegativeInfinity = double.NegativeInfinity,
                    NotANumber = double.NaN,
                    Result = p!.A + p.B
                };
            });
            
            rpc.AddMethod<object, object>("echo", (p, ctx) => p);
            
            // Simulate handling a request
            var request = @"{
                ""jsonrpc"": ""2.0"",
                ""method"": ""mathOperations"",
                ""params"": { ""a"": 10, ""b"": 5 },
                ""id"": 1
            }";
            
            var response = await rpc.HandleRequestAsync(request);
            Console.WriteLine("Server Response:");
            Console.WriteLine(response);
            Console.WriteLine();
            
            // ===== CLIENT SIDE =====
            
            // Create a safe client (Safe Mode enabled by default)
            var client = new RpcSafeClient("http://localhost:5000/api");
            
            try
            {
                // Call methods
                var time = await client.CallAsync<object>("getCurrentTime");
                Console.WriteLine($"Current time: {time}");
                
                var math = await client.CallAsync<object>("mathOperations", 
                    new { A = 10, B = 5 });
                Console.WriteLine($"Math operations: {math}");
                
                var echo = await client.CallAsync<object>("echo", 
                    new { Test = "value", Number = 42 });
                Console.WriteLine($"Echo: {echo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
            
            // You can still override options if needed
            var customOptions = new RpcClientOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
                // SafeEnabled is still true by default
            };
            
            var customClient = new RpcSafeClient("http://localhost:5000/api", customOptions);
            
            Console.WriteLine();
            Console.WriteLine("=== Safe Mode Classes Demo ===");
            Console.WriteLine("RpcSafeEndpoint: Automatically enables Safe Mode for the server");
            Console.WriteLine("RpcSafeClient: Automatically enables Safe Mode for the client");
            Console.WriteLine("Both classes provide a cleaner API compared to manual option setting");
            
            customClient.Dispose();
        }
    }
    
    class MathParams
    {
        public double A { get; set; }
        public double B { get; set; }
    }
}
