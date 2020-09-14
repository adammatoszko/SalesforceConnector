namespace SalesforceConnector.Services
{
    internal static class HttpMessageServiceConsts
    {
        internal const string ENDPOINT_START = "<serverUrl>";
        internal const string ENDPOINT_END = "/services/Soap/c/";
        internal const string SESSION_ID_START = "<sessionId>";
        internal const string SESSION_ID_END = "</sessionId>";
        internal const string MEDIA_TYPE_XML = "text/xml";
        internal const string MEDIA_TYPE_JSON = "application/json";
        internal const string CHARSET = "utf-8";
        internal const string SOAP_ACTION_KEY = "SOAPAction";
        internal const string SOAP_ACTION_VALUE = "\"\"";
        internal const string QUERY_URL = "/services/data/v{0}/query/?q=";
        internal const string UPDATE_URL = "/services/data/v{0}/composite/sobjects";
        internal const string LOGIN_URL = "https://{0}.salesforce.com/services/Soap/c/{1}/";
        internal const string LOGOUT_URL = "https://{0}.salesforce.com/services/oauth2/revoke?token=";
        internal const string ALL_OR_NONE_FALSE = "&allOrNone=false";
        internal const string ALL_OR_NONE_TRUE = "&allOrNone=true";
        internal const string IDS = "?ids=";
    }
}
