using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public struct QueryResultModel<T>
    {
        [DataMember(Name = "totalSize")]
        public int TotalSize { get; set; }

        [DataMember(Name = "done")]
        public bool Done { get; set; }

        [DataMember(Name = "records")]
        public T[] Records { get; set; }

        [DataMember(Name = "nextRecordsUrl")]
        public string NextRecords { get; set; }
    }
}
