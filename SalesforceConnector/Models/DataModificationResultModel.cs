using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public struct DataModificationResultModel
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "errors")]
        public SalesforceErrorModel[] Errors { get; set; }
    }
}
