using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace SalesforceConnector.Models
{
    /// <summary>
    /// Represents errors during data modification.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct SalesforceErrorModel
    {
        /// <summary>
        /// Error status code.
        /// </summary>
        [DataMember(Name = "statusCode")]
        public string StatusCode { get; }

        /// <summary>
        /// Error message.
        /// </summary>
        [DataMember(Name = "message")]
        public string Message { get; }

        /// <summary>
        /// Object fields that are related to the error.
        /// </summary>
        [DataMember(Name = "fields")]
        public string[] Fields { get; }

        [SerializationConstructor]
        internal SalesforceErrorModel(string statusCode, string message, string[] fields)
        {
            StatusCode = statusCode;
            Message = message;
            Fields = fields;
        }
    }
}