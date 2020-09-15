using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    /// <summary>
    /// Represents Salesforce record type attribute.
    /// </summary>
    public readonly struct SalesforceAttributeModel
    {
        /// <summary>
        /// Salesforce object type.
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; }

        public SalesforceAttributeModel(string type)
        {
            Type = type;
        }
    }
}
