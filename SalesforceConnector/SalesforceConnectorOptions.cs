using Microsoft.Extensions.Options;

namespace SalesforceConnector
{
    public class SalesforceConnectorOptions : IOptions<SalesforceConnectorOptions>
    {
        public string LoginEndpoint { get; set; }
        public string LogoutEndpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool AllOrNone { get; set; }
        public string HttpClientInstanceName { get; set; }

        public SalesforceConnectorOptions Value => this;
    }
}
