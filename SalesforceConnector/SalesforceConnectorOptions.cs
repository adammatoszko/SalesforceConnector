namespace SalesforceConnector
{
    /// <summary>
    /// Provides configuration options for the connector.
    /// </summary>
    public class SalesforceConnectorOptions
    {
        /// <summary>
        /// Version of the API to use.
        /// </summary>
        public string ApiVersion { get; set; }

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

        /// <summary>
        /// True to use login.salesforce.com, false to use test.salesforce.com;
        /// </summary>
        public bool IsProduction { get; set; }
    }
}
