using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesforceConnector.Enums;
using SalesforceConnector.Models;
using SalesforceConnector.Models.Internals;
using SalesforceConnector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("SalesforceConnector.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#pragma warning disable CS1591

namespace SalesforceConnector.Client
{
    public class SalesforceClient : ISalesforceClient
    {
        private int _taken;
        private static Lazy<HttpClient> _clientInitializer;
        private readonly ILogger<SalesforceClient> _logger;
        private readonly IHttpMessageService _messageService;
        private readonly HttpClient _client;

        internal SalesforceClient(IHttpMessageService messageService, IOptions<SalesforceConnectorOptions> options, IHttpClientFactory clientFactory = default, ILogger<SalesforceClient> logger = default, HttpClient client = default)
        {
            _logger = logger;
            _messageService = messageService;
            _client = options.Value.HttpClientInstanceName is null ? client : clientFactory?.CreateClient(options.Value.HttpClientInstanceName);
            _client ??= _clientInitializer.Value;
        }

        static SalesforceClient()
        {
            _clientInitializer = new Lazy<HttpClient>(() => new HttpClient(), true);
        }

        public async Task LogInAsync(CancellationToken token = default)
        {
            try
            {
                if (Interlocked.Increment(ref _taken) == 1)
                {
                    _logger?.LogDebug("Logging in...");
                    HttpRequestMessage message = _messageService.BuildLoginMessage();
                    HttpResponseMessage response = await _client.SendAsync(message, token).ConfigureAwait(false);
                    await _messageService.ProcessLoginResponseAsync(response, token).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("A login operation is already in progress.");
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Interlocked.Decrement(ref _taken);
            }
        }

        public async Task LogOutAsync(CancellationToken token = default)
        {
            try
            {
                _logger?.LogDebug("Logging out...");
                HttpRequestMessage message = _messageService.BuildLogoutMessage();
                HttpResponseMessage response = await _client.SendAsync(message, token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Log out failed - received status code {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (OperationCanceledException) { }
        }

        public ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(IEnumerable<T> records, DataModificationType modificationType, bool allOrNone, CancellationToken token = default) where T : SalesforceObjectModel =>
            ModifyDataAsync<T>(records.ToArray(), modificationType, allOrNone, token);

        public async ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(T[] records, DataModificationType modificationType, bool allOrNone, CancellationToken token = default) where T : SalesforceObjectModel
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (records == null || records.Length == 0)
                {
                    return Enumerable.Empty<DataModificationResultModel>().ToList();
                }

                HttpMethod method = modificationType switch
                {
                    DataModificationType.Insert => HttpMethod.Post,
                    DataModificationType.Update => HttpMethod.Patch,
                    DataModificationType.Delete => HttpMethod.Delete,
                    _ => throw new ArgumentException("Unknown data modification type")
                };
                List<DataModificationResultModel> results = new List<DataModificationResultModel>(records.Length);

                for (int i = 0; i < records.Length; i += 200)
                {
                    Range r = i + 200 < records.Length ? new Range(i, i + 200) : new Range(i, records.Length);
                    T[] current = records[r];
                    _logger?.LogDebug($"Updating {current.Length} records of type {typeof(T)}");
                    HttpRequestMessage message = await _messageService.BuildDataChangeMessageAsync<T>(current, method, allOrNone, token).ConfigureAwait(false);
                    HttpResponseMessage response = await _client.SendAsync(message, token).ConfigureAwait(false);
                    DataModificationResultModel[] res = await _messageService.ProcessResponseAsync<DataModificationResultModel[]>(response, token).ConfigureAwait(false);
                    results.AddRange(res);
                }
                _logger?.LogInformation(DataModificationResultLogString(results));
                return results;
            }
            catch (OperationCanceledException)
            {
                return await Task.FromCanceled<List<DataModificationResultModel>>(token).ConfigureAwait(false);
            }
        }

        public async Task<T[]> QueryDataAsync<T>(string soql, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                _logger?.LogDebug($"Running SOQL query: {soql}");
                HttpRequestMessage request;
                HttpResponseMessage response;
                QueryResultModel<T> responseResult = default;
                bool queryComplete = true;
                T[] result = null;
                do
                {
                    request = _messageService.BuildQueryMessage(queryComplete ? soql : responseResult.NextRecordsUrl, !queryComplete);
                    response = await _client.SendAsync(request, token).ConfigureAwait(false);
                    responseResult = await _messageService.ProcessResponseAsync<QueryResultModel<T>>(response, token).ConfigureAwait(false);
                    queryComplete = responseResult.Done;
                    if (!queryComplete || result != null)
                    {
                        result ??= new T[responseResult.TotalSize];
                        Array.Copy(responseResult.Records, 0, result, FindFirstNull(result), responseResult.Records.Length);
                    }
                } while (!queryComplete);

                return result ?? responseResult.Records;
            }
            catch (OperationCanceledException)
            {
                return await Task.FromCanceled<T[]>(token).ConfigureAwait(false);
            }
        }

        private int FindFirstNull<T>(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException("No null elements in the query result array. Cannot complete query operation.");
        }

        private string DataModificationResultLogString(List<DataModificationResultModel> resultCollection)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Operation results:" + Environment.NewLine);
            for (int i = 0; i < resultCollection.Count; i++)
            {
                sb.Append(i).Append(" - ")
                  .Append("Record Id - ").Append(resultCollection[i].Id ?? "null").Append(" - ")
                  .Append("Success - ").Append(resultCollection[i].Success)
                  .Append(Environment.NewLine).Append("Errors:" + Environment.NewLine).Append(resultCollection[i].Errors.Length);
                if (resultCollection[i].Errors.Length > 0)
                {
                    for (int j = 0; j < resultCollection[i].Errors.Length; j++)
                    {
                        sb.Append(resultCollection[i].Errors[j].StatusCode).Append(" - ").Append(resultCollection[i].Errors[j].Message).Append(Environment.NewLine);
                        if (resultCollection[i].Errors[j].Fields.Length > 0)
                        {
                            sb.Append("Fields:" + Environment.NewLine);
                            for (int k = 0; k < resultCollection[i].Errors[j].Fields.Length; k++)
                            {
                                sb.Append(resultCollection[i].Errors[j].Fields[k]);
                                if (k != resultCollection[i].Errors[j].Fields.Length - 1)
                                {
                                    sb.Append(" - ");
                                }
                            }
                        }
                    }
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
