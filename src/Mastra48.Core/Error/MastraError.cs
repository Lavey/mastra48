using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mastra48.Error
{
    /// <summary>
    /// Defines the structure for an error's metadata.
    /// Mirrors IErrorDefinition from packages/core/src/error/index.ts
    /// </summary>
    public class ErrorDefinition
    {
        /// <summary>Unique identifier for the error (uppercase convention).</summary>
        public string Id { get; set; }

        /// <summary>Optional custom error message that overrides the original error message.</summary>
        public string Text { get; set; }

        /// <summary>Functional domain of the error.</summary>
        public ErrorDomain Domain { get; set; }

        /// <summary>Broad category of the error.</summary>
        public ErrorCategory Category { get; set; }

        /// <summary>Additional error details as key-value pairs.</summary>
        public Dictionary<string, object> Details { get; set; }
    }

    /// <summary>
    /// JSON representation of a MastraError for serialization.
    /// Mirrors MastraErrorJSON from packages/core/src/error/index.ts
    /// </summary>
    public class MastraErrorJson
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("details")]
        public Dictionary<string, object> Details { get; set; }

        [JsonProperty("cause")]
        public MastraErrorJson Cause { get; set; }
    }

    /// <summary>
    /// Base error class for the Mastra ecosystem.
    /// Mirrors MastraBaseError / MastraError from packages/core/src/error/index.ts
    /// </summary>
    public class MastraError : Exception
    {
        /// <summary>Unique identifier for the error.</summary>
        public string Id { get; }

        /// <summary>Functional domain of the error.</summary>
        public ErrorDomain Domain { get; }

        /// <summary>Broad category of the error.</summary>
        public ErrorCategory Category { get; }

        /// <summary>Additional error details.</summary>
        public Dictionary<string, object> Details { get; }

        /// <param name="definition">Error definition with id, domain, category, text, details.</param>
        /// <param name="innerException">Optional original exception that caused this error.</param>
        public MastraError(ErrorDefinition definition, Exception innerException = null)
            : base(ResolveMessage(definition, innerException), innerException)
        {
            Id = definition.Id ?? "UNKNOWN";
            Domain = definition.Domain;
            Category = definition.Category;
            Details = definition.Details ?? new Dictionary<string, object>();
        }

        private static string ResolveMessage(ErrorDefinition def, Exception inner)
        {
            if (def == null) return "Unknown error";
            if (!string.IsNullOrEmpty(def.Text)) return def.Text;
            if (inner != null) return inner.Message;
            return "Unknown error";
        }

        /// <summary>
        /// Returns a structured representation of the error, useful for logging or API responses.
        /// </summary>
        public MastraErrorJson ToJson()
        {
            return new MastraErrorJson
            {
                Message = Message,
                Code = Id,
                Category = Category.ToString(),
                Domain = Domain.ToString(),
                Details = Details,
                Cause = InnerException != null
                    ? new MastraErrorJson { Message = InnerException.Message }
                    : null
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(ToJson());
        }
    }
}
