namespace SalesforceConnector.Models
{
    internal static class SoapAuthenticationModel
    {
        private const string LOGIN_MESSAGE = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "<s:Body xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
            "<login xmlns=\"urn:enterprise.soap.sforce.com\">" +
            "<username>{0}</username>" +
            "<password>{1}</password>" +
            "</login></s:Body>" +
            "</s:Envelope>";

        public static string GetLoginMessage(string username, string password) => string.Format(LOGIN_MESSAGE, username, password);
    }
}