using SalesforceConnector.Enums;
using SalesforceConnector.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesforceConnector.Client
{
    public interface ISalesforceClient
    {
        /// <summary>
        /// Logs into Salesforce and creates a session.
        /// </summary>
        Task LogInAsync();

        /// <summary>
        /// Logs out of Salesforce and invalidates the session.
        /// </summary>
        Task LogOutAsync();

        /// <summary>
        /// Runs the provided SOQL query.
        /// </summary>
        /// <typeparam name="T">Type of records to return.</typeparam>
        /// <param name="soql">SOQL query to run.</param>
        /// <returns>Array of query result records.</returns>
        Task<T[]> QueryDataAsync<T>(string soql);

        /// <summary>
        /// Modifies data in Salesforce.
        /// </summary>
        /// <typeparam name="T">Type of records to modify. Must be SalesforceObjectModel derived.</typeparam>
        /// <param name="records">The records to be modified.</param>
        /// <param name="modificationType">Type of modification to conduct.</param>
        /// <param name="allOrNone">True if entire batch should fail if one record fails.</param>
        /// <returns>Results of the modification.</returns>
        ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(T[] records, DataModificationType modificationType, bool allOrNone) where T : SalesforceObjectModel;

        /// <summary>
        /// Modifies data in Salesforce.
        /// </summary>
        /// <typeparam name="T">Type of records to modify. Must be SalesforceObjectModel derived.</typeparam>
        /// <param name="records">The records to be modified.</param>
        /// <param name="modificationType">Type of modification to conduct.</param>
        /// <param name="allOrNone">True if entire batch should fail if one record fails.</param>
        /// <returns>Results of the modification.</returns>
        ValueTask<List<DataModificationResultModel>> ModifyDataAsync<T>(IEnumerable<T> records, DataModificationType modificationType, bool allOrNone) where T : SalesforceObjectModel;
    }
}