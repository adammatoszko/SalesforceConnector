using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesforceConnector.Enums;
using SalesforceConnector.Models;
using SalesforceConnector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("SalesforceConnector.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

namespace SalesforceConnector.Client
{
    public class SalesforceClient : ISalesforceClient
    {
        private static Lazy<HttpClient> _clientInitializer;
        private readonly ILogger<SalesforceClient> _logger;
        private readonly IHttpMessageService _messageService;
        private readonly HttpClient _client;

        public SalesforceClient(IHttpMessageService messageService, IOptions<SalesforceConnectorOptions> options, IHttpClientFactory clientFactory = default, ILogger<SalesforceClient> logger = default, HttpClient client = default)
        {
            _logger = logger;
            _messageService = messageService;
            _client = options.Value.HttpClientInstanceName is null ? client : clientFactory?.CreateClient(options.Value.HttpClientInstanceName);
            _client ??= _clientInitializer.Value;
        }

        static SalesforceClient()
        {
            _clientInitializer = new Lazy<HttpClient>(CreateClient, true);
        }

        private static HttpClient CreateClient() => new HttpClient();

        public async Task LogInAsync()
        {
            _logger?.LogDebug("Logging in...");
            HttpRequestMessage message = _messageService.BuildLoginMessage();
            HttpResponseMessage response = await _client.SendAsync(message).ConfigureAwait(false);
            await _messageService.ProcessLoginResponseAsync(response).ConfigureAwait(false);
        }

        public async Task LogOutAsync()
        {
            _logger?.LogDebug("Logging out...");
            HttpRequestMessage message = _messageService.BuildLogoutMessage();
            HttpResponseMessage response = await _client.SendAsync(message).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Log out failed - received status code {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }

        public ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(IEnumerable<T> records, DataModificationType modificationType, bool allOrNone) where T : SalesforceObjectModel =>
            ModifyDataAsync<T>(records.ToArray(), modificationType, allOrNone);

        public async ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(T[] records, DataModificationType modificationType, bool allOrNone) where T : SalesforceObjectModel
        {
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
                HttpRequestMessage message = await _messageService.BuildDataChangeMessageAsync<T>(current, method, allOrNone).ConfigureAwait(false);
                HttpResponseMessage response = await _client.SendAsync(message).ConfigureAwait(false);
                DataModificationResultModel[] res = await _messageService.ProcessResponseAsync<DataModificationResultModel[]>(response).ConfigureAwait(false);
                results.AddRange(res);
            }
            _logger?.LogInformation(DataModificationResultLogString(results));
            return results;
        }

        public async Task<T[]> QueryDataAsync<T>(string soql)
        {
            _logger?.LogDebug($"Running SOQL query: {soql}");
            HttpRequestMessage request;
            HttpResponseMessage response;
            QueryResultModel<T> responseResult = default;
            bool queryComplete = true;
            T[] result = null;
            do
            {
                request = _messageService.BuildQueryMessage(queryComplete ? soql : responseResult.NextRecords, !queryComplete);
                response = await _client.SendAsync(request).ConfigureAwait(false);
                responseResult = await _messageService.ProcessResponseAsync<QueryResultModel<T>>(response).ConfigureAwait(false);
                queryComplete = responseResult.Done;
                if (!queryComplete || result != null)
                {
                    result ??= new T[responseResult.TotalSize];
                    Array.Copy(responseResult.Records, 0, result, FindFirstNull(result), responseResult.Records.Length);
                }
            } while (!queryComplete);

            return result ?? responseResult.Records;
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
            throw new ArgumentOutOfRangeException("No null elements in the array");
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
