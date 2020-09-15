using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct DataModificationResultModel
    {
        [DataMember(Name = "id")]
        public string Id { get; }

        [DataMember(Name = "success")]
        public bool Success { get; }

        [DataMember(Name = "errors")]
        public SalesforceErrorModel[] Errors { get; }

        [SerializationConstructor]
        public DataModificationResultModel(string id, bool success, SalesforceErrorModel[] errors)
        {
            Id = id;
            Success = success;
            Errors = errors;
        }
    }
}
