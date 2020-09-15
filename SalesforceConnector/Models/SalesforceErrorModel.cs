using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct SalesforceErrorModel
    {
        [DataMember(Name = "statusCode")]
        public string StatusCode { get; }

        [DataMember(Name = "message")]
        public string Message { get; }

        [DataMember(Name = "fields")]
        public string[] Fields { get; }

        [SerializationConstructor]
        public SalesforceErrorModel(string statusCode, string message, string[] fields)
        {
            StatusCode = statusCode;
            Message = message;
            Fields = fields;
        }
    }
}