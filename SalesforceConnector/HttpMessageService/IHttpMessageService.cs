using SalesforceConnector.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace SalesforceConnector.Services
{
    public interface IHttpMessageService
    {
        HttpRequestMessage BuildLoginMessage();
        Task ProcessLoginResponseAsync(HttpResponseMessage response);
        HttpRequestMessage BuildQueryMessage(string soql, in bool isQueryMore);
        Task<T> ProcessResponseAsync<T>(HttpResponseMessage response);
        HttpRequestMessage BuildLogoutMessage();
        Task<HttpRequestMessage> BuildDataChangeMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone) where T : SalesforceObjectModel;
    }
}