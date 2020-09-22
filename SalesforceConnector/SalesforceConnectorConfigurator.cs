using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesforceConnector.Client;
using SalesforceConnector.Services;
using System;
using System.Net.Http;

#pragma warning disable CS1591

namespace SalesforceConnector
{
    public static class SalesforceConnectorConfigurator
    {
        /// <summary>
        /// Configures the SalesforceConnector client.
        /// </summary>
        /// <param name="collection">DI collection</param>
        /// <param name="optionsConfigurator">Configuration delegate</param>
        public static IServiceCollection AddSalesforceConnector(this IServiceCollection collection, Action<SalesforceConnectorOptions> optionsConfigurator)
        {
            collection.Configure<SalesforceConnectorOptions>(optionsConfigurator);
            collection
                .AddSingleton<IHttpMessageService, HttpMessageService>()
                .AddSingleton<ISalesforceClient, SalesforceClient>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<SalesforceConnectorOptions>>();
                    var messageService = provider.GetRequiredService<IHttpMessageService>();
                    var sfClientLogger = provider.GetService<ILogger<SalesforceClient>>();
                    var httpClientFactory = provider.GetService<IHttpClientFactory>();
                    var httpClient = provider.GetService<HttpClient>();
                    return new SalesforceClient(messageService, options, httpClientFactory, sfClientLogger, httpClient);
                });

            return collection;
        }
    }
}
