using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace SalesforceConnector.Models.Internals
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct QueryResultModel<T>
    {
        [DataMember(Name = "totalSize")]
        public int TotalSize { get; }

        [DataMember(Name = "done")]
        public bool Done { get; }

        [DataMember(Name = "records")]
        public T[] Records { get; }

        [DataMember(Name = "nextRecordsUrl")]
        public string NextRecordsUrl { get; }

        [SerializationConstructor]
        public QueryResultModel(int totalSize, bool done, T[] records, string nextRecordsUrl)
        {
            TotalSize = totalSize;
            Done = done;
            Records = records;
            NextRecordsUrl = nextRecordsUrl;
        }
    }
}
