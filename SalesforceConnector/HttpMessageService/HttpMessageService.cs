using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesforceConnector.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
        private readonly string _updateEndpoint;
        private readonly string _queryEndpoint;
        private readonly string _loginEndpoint;
        private readonly string _logoutEndpoint;
        private AuthenticationHeaderValue _authHeader;

        public HttpMessageService(IOptions<SalesforceConnectorOptions> options, ILogger<HttpMessageService> logger = default)
        {
            _options = options;
            _logger = logger;
            string environment = _options.Value.IsProduction ? "login" : "test";
            _loginEndpoint = string.Format(HttpMessageServiceConsts.LOGIN_URL, environment, _options.Value.ApiVersion);
            _logoutEndpoint = string.Format(HttpMessageServiceConsts.LOGOUT_URL, environment);
            _updateEndpoint = string.Format(HttpMessageServiceConsts.UPDATE_URL, _options.Value.ApiVersion);
            _queryEndpoint = string.Format(HttpMessageServiceConsts.QUERY_URL, _options.Value.ApiVersion);
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

        public async Task ProcessLoginResponseAsync(HttpResponseMessage response, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await CheckStatusCodeAsync(response).ConfigureAwait(false);
            ReadOnlyMemory<byte> responseContent = (await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)).AsMemory();
            _sessionId = ExtractElement(in responseContent, HttpMessageServiceConsts.SESSION_ID_START, HttpMessageServiceConsts.SESSION_ID_END);
            _requestEndpoint = ExtractElement(in responseContent, HttpMessageServiceConsts.ENDPOINT_START, HttpMessageServiceConsts.ENDPOINT_END);
            _authHeader = new AuthenticationHeaderValue("Bearer", _sessionId);
            _logger?.LogDebug($"Received endpoint {_requestEndpoint} and session id {_sessionId}");
        }

        public HttpRequestMessage BuildQueryMessage(string query, in bool isQueryMore)
        {
            string requestUri = isQueryMore
                              ? _requestEndpoint + query
                              : _requestEndpoint + _queryEndpoint + query;
            return BuildBasicMessage(HttpMethod.Get, requestUri);
        }

        public async Task<HttpRequestMessage> BuildDataChangeMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone, CancellationToken token) where T : SalesforceObjectModel
        {
            token.ThrowIfCancellationRequested();
            if (method == HttpMethod.Post || method == HttpMethod.Patch)
            {
                return await BuildPostPatchMessageAsync(records, method, allOrNone, token).ConfigureAwait(false);
            }
            else if (method == HttpMethod.Delete)
            {
                return BuildDeleteMessage(records, allOrNone);
            }
            throw new HttpRequestException($"HttpMethod {method.Method} is not supported");
        }

        public async Task<T> ProcessResponseAsync<T>(HttpResponseMessage message, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await CheckStatusCodeAsync(message).ConfigureAwait(false);
            Stream contentStream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
            T result = await JsonSerializer.DeserializeAsync<T>(contentStream);
            return result;
        }

        private HttpRequestMessage BuildDeleteMessage<T>(T[] records, bool allOrNone) where T : SalesforceObjectModel
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_requestEndpoint)
              .Append(_updateEndpoint)
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
            return BuildBasicMessage(HttpMethod.Delete, sb.ToString());
        }

        private async Task<HttpRequestMessage> BuildPostPatchMessageAsync<T>(T[] records, HttpMethod method, bool allOrNone, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ObjectsUpdateModel<T> objects = new ObjectsUpdateModel<T>()
            {
                AllOrNone = allOrNone,
                Records = records
            };
            Stream str = new MemoryStream();
            JsonSerializer.SetDefaultResolver(StandardResolver.AllowPrivateExcludeNull);
            await JsonSerializer.SerializeAsync(str, objects).ConfigureAwait(false);
            str.Position = 0;
            HttpRequestMessage message = BuildBasicMessage(method, _requestEndpoint + _updateEndpoint);
            message.Content = new StreamContent(str);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue(HttpMessageServiceConsts.MEDIA_TYPE_JSON);
            message.Content.Headers.ContentType.CharSet = HttpMessageServiceConsts.CHARSET;
            return message;
        }

        private HttpRequestMessage BuildBasicMessage(HttpMethod method, string endpoint)
        {
            HttpRequestMessage message = new HttpRequestMessage(method, endpoint);
            message.Headers.Authorization = _authHeader;
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

        private string ExtractElement(in ReadOnlyMemory<byte> responseBytes, string startSequence, string endSequence)
        {
            ReadOnlySpan<byte> start = Encoding.UTF8.GetBytes(startSequence).AsSpan();
            ReadOnlySpan<byte> end = Encoding.UTF8.GetBytes(endSequence).AsSpan();
            FindIndexes(in responseBytes, in start, in end, out int startLocation, out int length);
            return Encoding.UTF8.GetString(responseBytes.Span.Slice(startLocation, length));
        }

        private void FindIndexes(in ReadOnlyMemory<byte> responseBytes, in ReadOnlySpan<byte> startSequence, in ReadOnlySpan<byte> endSequence, out int start, out int length)
        {
            start = responseBytes.Span.IndexOf(startSequence) + startSequence.Length;
            int end = responseBytes.Span.IndexOf(endSequence);
            length = end - start;
        }
    }
}
