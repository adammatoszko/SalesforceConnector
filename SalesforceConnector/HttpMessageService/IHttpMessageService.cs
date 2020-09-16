using SalesforceConnector.Models;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SalesforceConnector.Services
{
    internal interface IHttpMessageService
    {
        HttpRequestMessage BuildLoginMessage();
        Task ProcessLoginResponseAsync(HttpResponseMessage response, CancellationToken token);
        HttpRequestMessage BuildQueryMessage(string soql, in bool isQueryMore);
        Task<T> ProcessResponseAsync<T>(HttpResponseMessage response, CancellationToken token);
        HttpRequestMessage BuildLogoutMessage();
        Task<HttpRequestMessage> BuildDataChangeMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone, CancellationToken token) where T : SalesforceObjectModel;
    }
}