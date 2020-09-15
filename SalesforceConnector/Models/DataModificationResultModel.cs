using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace SalesforceConnector.Models
{
    /// <summary>
    /// Represents the result of data modification in Salesforce.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct DataModificationResultModel
    {
        /// <summary>
        /// The id of the modified record.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; }

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        [DataMember(Name = "success")]
        public bool Success { get; }

        /// <summary>
        /// Data modification errors, if any.
        /// </summary>
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
