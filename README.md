# SalesforceConnector
Simple library allowing to connect to Salesforce and run SOQL queries

This library is meant to be used with a DI container: 
>IServiceCollection.AddSalesforceConnector(options => { //configure options here });

If the container has a IHttpClientFactory that contains a client with a specified name, the library can use it, provided it received the name in the config delegate. If not, it will create its own client instance.
