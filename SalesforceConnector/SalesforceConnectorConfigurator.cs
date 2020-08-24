using Microsoft.Extensions.DependencyInjection;
using SalesforceConnector.Client;
using SalesforceConnector.Services;
using System;

namespace SalesforceConnector
{
    public static class SalesforceConnectorConfigurator
    {
        public static IServiceCollection AddSalesforceConnector(this IServiceCollection collection, Action<SalesforceConnectorOptions> optionsConfigurator)
        {
            collection.Configure<SalesforceConnectorOptions>(optionsConfigurator);
            collection
                .AddSingleton<ISalesforceClient, SalesforceClient>()
                .AddSingleton<IHttpMessageService, HttpMessageService>();
            return collection;
        }
    }
}
