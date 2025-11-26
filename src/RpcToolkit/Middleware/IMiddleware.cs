using System.Threading.Tasks;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Interface for RPC middleware
    /// </summary>
    public interface IMiddleware
    {
        /// <summary>
        /// Execute before the RPC method is called
        /// </summary>
        Task BeforeAsync(RpcRequest request, object? context);

        /// <summary>
        /// Execute after the RPC method is called
        /// </summary>
        Task AfterAsync(RpcRequest request, object? result, object? context);
    }
}
