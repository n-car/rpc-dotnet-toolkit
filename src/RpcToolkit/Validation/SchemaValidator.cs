using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.Validation;

namespace RpcToolkit.Validation
{
    /// <summary>
    /// JSON Schema validator for RPC methods
    /// </summary>
    public class SchemaValidator
    {
        private readonly Dictionary<string, MethodSchema> _schemas = new();

        /// <summary>
        /// Register schema for a method
        /// </summary>
        public SchemaValidator AddMethodSchema(
            string methodName,
            JsonSchema? paramsSchema = null,
            JsonSchema? resultSchema = null)
        {
            _schemas[methodName] = new MethodSchema
            {
                ParamsSchema = paramsSchema,
                ResultSchema = resultSchema
            };
            return this;
        }

        /// <summary>
        /// Register schema from JSON string
        /// </summary>
        public async Task<SchemaValidator> AddMethodSchemaFromJsonAsync(
            string methodName,
            string? paramsSchemaJson = null,
            string? resultSchemaJson = null)
        {
            JsonSchema? paramsSchema = null;
            JsonSchema? resultSchema = null;

            if (!string.IsNullOrEmpty(paramsSchemaJson))
            {
                paramsSchema = await JsonSchema.FromJsonAsync(paramsSchemaJson);
            }

            if (!string.IsNullOrEmpty(resultSchemaJson))
            {
                resultSchema = await JsonSchema.FromJsonAsync(resultSchemaJson);
            }

            return AddMethodSchema(methodName, paramsSchema, resultSchema);
        }

        /// <summary>
        /// Generate schema from .NET type
        /// </summary>
        public SchemaValidator AddMethodSchemaFromType<TParams, TResult>(string methodName)
        {
            var paramsSchema = JsonSchema.FromType<TParams>();
            var resultSchema = JsonSchema.FromType<TResult>();
            return AddMethodSchema(methodName, paramsSchema, resultSchema);
        }

        /// <summary>
        /// Validate method parameters
        /// </summary>
        public async Task<ValidationResult> ValidateParamsAsync(string methodName, string paramsJson)
        {
            if (!_schemas.TryGetValue(methodName, out var methodSchema))
            {
                return new ValidationResult { IsValid = true }; // No schema = no validation
            }

            if (methodSchema.ParamsSchema == null)
            {
                return new ValidationResult { IsValid = true };
            }

            var validator = new JsonSchemaValidator();
            var errors = validator.Validate(paramsJson, methodSchema.ParamsSchema);

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validate method result
        /// </summary>
        public async Task<ValidationResult> ValidateResultAsync(string methodName, string resultJson)
        {
            if (!_schemas.TryGetValue(methodName, out var methodSchema))
            {
                return new ValidationResult { IsValid = true };
            }

            if (methodSchema.ResultSchema == null)
            {
                return new ValidationResult { IsValid = true };
            }

            var validator = new JsonSchemaValidator();
            var errors = validator.Validate(resultJson, methodSchema.ResultSchema);

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Check if method has schema defined
        /// </summary>
        public bool HasSchema(string methodName)
        {
            return _schemas.ContainsKey(methodName);
        }

        /// <summary>
        /// Get schema for method
        /// </summary>
        public MethodSchema? GetSchema(string methodName)
        {
            return _schemas.TryGetValue(methodName, out var schema) ? schema : null;
        }
    }

    /// <summary>
    /// Schema definition for a method
    /// </summary>
    public class MethodSchema
    {
        /// <summary>
        /// JSON schema for validating method parameters
        /// </summary>
        public JsonSchema? ParamsSchema { get; set; }
        
        /// <summary>
        /// JSON schema for validating method results
        /// </summary>
        public JsonSchema? ResultSchema { get; set; }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Indicates whether the validation passed</summary>
        public bool IsValid { get; set; }
        
        /// <summary>Collection of validation errors (empty if valid)</summary>
        public ICollection<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Gets a formatted error message containing all validation errors
        /// </summary>
        /// <returns>Formatted error message, or empty string if valid</returns>
        public string GetErrorMessage()
        {
            if (IsValid) return string.Empty;

            var messages = new List<string>();
            foreach (var error in Errors)
            {
                messages.Add($"{error.Path}: {error.Kind}");
            }
            return string.Join("; ", messages);
        }
    }
}
