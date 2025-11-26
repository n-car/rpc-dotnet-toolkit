using System.Threading.Tasks;
using RpcToolkit.Exceptions;
using RpcToolkit.Validation;

namespace RpcToolkit.Middleware
{
    /// <summary>
    /// Middleware for validating RPC requests and responses against JSON Schema
    /// </summary>
    public class SchemaValidationMiddleware : IMiddleware
    {
        private readonly SchemaValidator _validator;
        private readonly bool _validateParams;
        private readonly bool _validateResult;

        /// <summary>
        /// Create schema validation middleware
        /// </summary>
        /// <param name="validator">Schema validator instance</param>
        /// <param name="validateParams">Whether to validate request parameters</param>
        /// <param name="validateResult">Whether to validate response results</param>
        public SchemaValidationMiddleware(
            SchemaValidator validator,
            bool validateParams = true,
            bool validateResult = false)
        {
            _validator = validator ?? throw new System.ArgumentNullException(nameof(validator));
            _validateParams = validateParams;
            _validateResult = validateResult;
        }

        /// <summary>
        /// Validates request parameters against defined JSON schema before processing
        /// </summary>
        /// <param name="request">The RPC request to validate</param>
        /// <param name="context">Request context</param>
        /// <exception cref="ValidationErrorException">Thrown when validation fails</exception>
        public async Task BeforeAsync(RpcRequest request, object? context)
        {
            if (!_validateParams) return;

            // Skip validation if no schema defined
            if (!_validator.HasSchema(request.Method)) return;

            // Serialize params to JSON for validation
            var paramsJson = request.Params != null
                ? System.Text.Json.JsonSerializer.Serialize(request.Params)
                : "null";

            var result = await _validator.ValidateParamsAsync(request.Method, paramsJson);

            if (!result.IsValid)
            {
                throw new ValidationErrorException(
                    $"Parameter validation failed: {result.GetErrorMessage()}",
                    new { errors = result.Errors }
                );
            }
        }

        /// <summary>
        /// Validates response result after processing
        /// </summary>
        /// <param name="request">The RPC request</param>
        /// <param name="result">The result to validate</param>
        /// <param name="context">Request context</param>
        public async Task AfterAsync(RpcRequest request, object? result, object? context)
        {
            if (!_validateResult) return;

            // Skip validation if no schema defined
            if (!_validator.HasSchema(request.Method)) return;

            // Serialize result to JSON for validation
            var resultJson = result != null
                ? System.Text.Json.JsonSerializer.Serialize(result)
                : "null";

            var validationResult = await _validator.ValidateResultAsync(request.Method, resultJson);

            if (!validationResult.IsValid)
            {
                throw new ValidationErrorException(
                    $"Result validation failed: {validationResult.GetErrorMessage()}",
                    new { errors = validationResult.Errors }
                );
            }
        }
    }
}
