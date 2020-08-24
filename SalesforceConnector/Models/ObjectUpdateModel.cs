using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    [StructLayout(LayoutKind.Auto)]
    public struct ObjectsUpdateModel<T>
    {
        [DataMember(Name = "allOrNone")]
        public bool AllOrNone { get; set; }

        [DataMember(Name = "records")]
        public T[] Records { get; set; }
    }
}
