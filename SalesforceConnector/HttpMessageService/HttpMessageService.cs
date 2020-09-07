using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesforceConnector.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace SalesforceConnector.Services
{
    internal sealed class HttpMessageService : IHttpMessageService
    {
        private string _sessionId;
        private string _requestEndpoint;
        private readonly IOptions<SalesforceConnectorOptions> _options;
        private readonly ILogger<HttpMessageService> _logger;
        private readonly string _loginEndpoint;
        private readonly string _logoutEndpoint;
        private static AuthenticationHeaderValue _authHeader;

        public HttpMessageService(IOptions<SalesforceConnectorOptions> options, ILogger<HttpMessageService> logger = default)
        {
            _options = options;
            _logger = logger;
            string environment = _options.Value.IsProduction ? "login" : "test";
            _loginEndpoint = $"https://{environment}.salesforce.com/services/Soap/c/{_options.Value.ApiVersion}/";
            _logoutEndpoint = $"https://{environment}.salesforce.com/services/oauth2/revoke?token=";
        }

        public HttpRequestMessage BuildLoginMessage()
        {
            _logger?.LogDebug($"Building log in message for endpoint {_loginEndpoint}");
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, _loginEndpoint);
            message.Content = new StringContent(SoapAuthenticationModel.GetLoginMessage(_options.Value.Username, _options.Value.Password));
            message.Content.Headers.ContentType.MediaType = HttpMessageServiceConsts.MEDIA_TYPE_XML;
            message.Content.Headers.ContentType.CharSet = HttpMessageServiceConsts.CHARSET;
            message.Headers.Add(HttpMessageServiceConsts.SOAP_ACTION_KEY, HttpMessageServiceConsts.SOAP_ACTION_VALUE);
            return message;
        }

        public HttpRequestMessage BuildLogoutMessage()
        {
            _logger?.LogDebug($"Builing logout message for session {_sessionId}");
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, _logoutEndpoint + _sessionId);
            return message;
        }

        public async Task ProcessLoginResponseAsync(HttpResponseMessage response)
        {
            await CheckStatusCodeAsync(response).ConfigureAwait(false);
            Memory<byte> responseContent = (await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).AsMemory();
            _sessionId = ExtractElement(in responseContent, HttpMessageServiceConsts.SESSION_ID_START, HttpMessageServiceConsts.SESSION_ID_END);
            _requestEndpoint = ExtractElement(in responseContent, HttpMessageServiceConsts.ENDPOINT_START, HttpMessageServiceConsts.ENDPOINT_END);
            _authHeader = new AuthenticationHeaderValue("Bearer", _sessionId);
            _logger?.LogDebug($"Received endpoint {_requestEndpoint} and session id {_sessionId}");
        }

        public HttpRequestMessage BuildQueryMessage(string query, in bool isQueryMore)
        {
            string requestUri = isQueryMore
                              ? string.Format("{0}{1}", _requestEndpoint, query)
                              : string.Format("{0}{1}{2}", _requestEndpoint, HttpMessageServiceConsts.REST_QUERY_URL, query);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, requestUri);
            message.Headers.Authorization = _authHeader;
            return message;
        }

        public async Task<HttpRequestMessage> BuildDataChangeMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone) where T : SalesforceObjectModel
        {
            if (method == HttpMethod.Post || method == HttpMethod.Patch)
            {
                return await BuildPostPatchMessageAsync(records, method, allOrNone).ConfigureAwait(false);
            }
            else if (method == HttpMethod.Delete)
            {
                return BuildDeleteMessage(records, allOrNone);
            }
            throw new HttpRequestException($"HttpMethod {method.Method} is not supported");
        }

        public async Task<T> ProcessResponseAsync<T>(HttpResponseMessage message)
        {
            await CheckStatusCodeAsync(message).ConfigureAwait(false);
            Stream contentStream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
            T result = await JsonSerializer.DeserializeAsync<T>(contentStream);
            return result;
        }

        private HttpRequestMessage BuildDeleteMessage<T>(T[] records, bool allOrNone) where T : SalesforceObjectModel
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_requestEndpoint)
              .Append(HttpMessageServiceConsts.UPDATE_URL)
              .Append(HttpMessageServiceConsts.IDS);
            for (int i = 0; i < records.Length; i++)
            {
                sb.Append(records[i].Id);
                if (i != records.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(allOrNone ? HttpMessageServiceConsts.ALL_OR_NONE_TRUE : HttpMessageServiceConsts.ALL_OR_NONE_FALSE);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, sb.ToString());
            message.Headers.Authorization = _authHeader;
            return message;
        }

        private async Task<HttpRequestMessage> BuildPostPatchMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone)
        {
            HttpRequestMessage message = new HttpRequestMessage(method, _requestEndpoint + HttpMessageServiceConsts.UPDATE_URL);
            ObjectsUpdateModel<T> objects = new ObjectsUpdateModel<T>()
            {
                AllOrNone = allOrNone,
                Records = records
            };
            Stream str = new MemoryStream();
            JsonSerializer.SetDefaultResolver(StandardResolver.AllowPrivateExcludeNull);
            await JsonSerializer.SerializeAsync(str, objects).ConfigureAwait(false);
            str.Position = 0;
            message.Content = new StreamContent(str);
            message.Headers.Authorization = _authHeader;
            message.Content.Headers.ContentType = new MediaTypeHeaderValue(HttpMessageServiceConsts.MEDIA_TYPE_JSON);
            message.Content.Headers.ContentType.CharSet = HttpMessageServiceConsts.CHARSET;
            return message;
        }

        private async Task CheckStatusCodeAsync(HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
            {
                string errorContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException(errorContent);
            }
        }

        private string ExtractElement(in Memory<byte> responseBytes, string startSequence, string endSequence)
        {
            Span<byte> start = Encoding.UTF8.GetBytes(startSequence).AsSpan();
            Span<byte> end = Encoding.UTF8.GetBytes(endSequence).AsSpan();
            FindIndexes(in responseBytes, in start, in end, out int startLocation, out int length);
            return Encoding.UTF8.GetString(responseBytes.Span.Slice(startLocation, length));
        }

        private void FindIndexes(in Memory<byte> responseBytes, in Span<byte> startSequence, in Span<byte> endSequence, out int start, out int length)
        {
            start = responseBytes.Span.IndexOf(startSequence) + startSequence.Length;
            int end = responseBytes.Span.IndexOf(endSequence);
            length = end - start;
        }
    }
}
