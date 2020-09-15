using System.Runtime.Serialization;

namespace SalesforceConnector.Models
{
    /// <summary>
    /// Represents a base Salesforce object model. All Salesforce object models should inherit from this class.
    /// </summary>
    public abstract class SalesforceObjectModel
    {
        /// <summary>
        /// Salesforce object id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Defines the Salesforce object type.
        /// </summary>
        [DataMember(Name = "attributes")]
        public SalesforceAttributeModel Attributes { get; }

        public SalesforceObjectModel(string type)
        {
            Attributes = new SalesforceAttributeModel(type);
        }
    }
}
