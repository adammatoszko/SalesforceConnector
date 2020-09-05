using Microsoft.Extensions.Options;

namespace SalesforceConnector
{
    /// <summary>
    /// Provides configuration options for the connector.
    /// </summary>
    public class SalesforceConnectorOptions
    {
        /// <summary>
        /// Enterprise WSDL login endpoint:
        /// https://[instance].salesforce.com/services/Soap/c/[version]/
        /// </summary>
        public string LoginEndpoint { get; set; }

        /// <summary>
        /// OAuth revoke endpoint:
        /// https://[instance].salesforce.com/services/oauth2/revoke?token=
        /// </summary>
        public string LogoutEndpoint { get; set; }

        /// <summary>
        /// Api user username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Api user password. Depending on settings, might require a security token:
        /// [password] or [password][securityToken]
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Name of HttpClient instance registered throught IHttpClientFactory.
        /// Null if client should not be used.
        /// </summary>
        public string HttpClientInstanceName { get; set; }

        /// <summary>
        /// True if failing to modify one record should fail the whole batch, otherwise false.
        /// </summary>
        public bool AllOrNone { get; set; }
    }
}
