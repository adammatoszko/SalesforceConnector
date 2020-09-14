using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    public readonly struct SalesforceAttributeModel
    {
        [DataMember(Name = "type")]
        public string Type { get; }

        public SalesforceAttributeModel(string objectType)
        {
            Type = objectType;
        }
    }
}
