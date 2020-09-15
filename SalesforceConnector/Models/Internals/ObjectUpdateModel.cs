using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SalesforceConnector.Models.Internals
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ObjectsUpdateModel<T>
    {
        [DataMember(Name = "allOrNone")]
        public bool AllOrNone { get; }

        [DataMember(Name = "records")]
        public T[] Records { get; }

        public ObjectsUpdateModel(bool allOrNone, T[] records)
        {
            AllOrNone = allOrNone;
            Records = records;
        }
    }
}
