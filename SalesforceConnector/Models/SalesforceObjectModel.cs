using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    public abstract class SalesforceObjectModel
    {
        public string Id { get; set; }

        [DataMember(Name = "attributes")]
        public SalesforceAttributeModel Attributes { get; }

        public SalesforceObjectModel(string type)
        {
            Attributes = new SalesforceAttributeModel(type);
        }
    }
}
