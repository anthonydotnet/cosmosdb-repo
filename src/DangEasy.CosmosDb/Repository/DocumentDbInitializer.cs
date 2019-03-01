using System;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;
using Microsoft.Azure.Documents.Client.TransientFaultHandling.Strategies;

namespace Dang.CosmosDb.Repository
{
    public class DocumentDbInitializer : IDocumentDbInitializer
    {
        public IReliableReadWriteDocumentClient GetClient(string endpointUrl, string authorizationKey, ConnectionPolicy connectionPolicy = null)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentNullException(nameof(endpointUrl));

            if (string.IsNullOrWhiteSpace(authorizationKey))
                throw new ArgumentNullException(nameof(authorizationKey));

            var documentClient = new DocumentClient(new Uri(endpointUrl), authorizationKey, connectionPolicy ?? new ConnectionPolicy());

            var documentRetryStrategy = new DocumentDbRetryStrategy(DocumentDbRetryStrategy.DefaultExponential) { FastFirstRetry = true };

            return documentClient.AsReliable(documentRetryStrategy);
        }
    }
}