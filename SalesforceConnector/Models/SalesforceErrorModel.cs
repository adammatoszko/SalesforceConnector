using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public struct SalesforceErrorModel
    {
        [DataMember(Name = "statusCode")]
        public string StatusCode { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "fields")]
        public string[] Fields { get; set; }
    }
}