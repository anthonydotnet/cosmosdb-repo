using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Dang.CosmosDb.Interfaces;
using StackExchange.Profiling;
using System.Collections.Generic;

namespace Dang.CosmosDb.Repositories
{
    public class CosmosDbRepository<T> : ICosmosDbRepository<T> where T : class
    {
        private readonly DocumentClient _client;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly Uri _collectionUri;

        const int MIN_THROUGHPUT = 400;

        public CosmosDbRepository(string endpointUri, string primaryKey, string databaseName, string collectionName)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);

            _client = new DocumentClient(new Uri(endpointUri), primaryKey);

            // ensure DB & collection exist
            EnsureContainers(databaseName, collectionName);
        }


        public CosmosDbRepository(string endpointUri, string primaryKey, Func<Database> databaseCreation, Func<Database, DocumentCollection> collectionCreation)
        {
            _client = new DocumentClient(new Uri(endpointUri), primaryKey);

            var database = databaseCreation();
            var collection = collectionCreation(database);

            _databaseName = database.Id;
            _collectionName = collection.Id;
            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collectionName);
        }

      
        private void EnsureContainers(string databaseName, string collectionName)
        {
           _client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName, }).Wait();

            var databaseUri = UriFactory.CreateDatabaseUri(databaseName);
            var collection = new DocumentCollection
            {
                Id = collectionName,
                //PartitionKey = new PartitionKeyDefinition
                //{
                //    Paths = new Collection<string> { "/id" }
                //},
                IndexingPolicy = new IndexingPolicy { IndexingMode = IndexingMode.Consistent },
            };

            _client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, collection, new RequestOptions { OfferThroughput = MIN_THROUGHPUT }).Wait();
        }

       

        protected FeedOptions EnsureFeedOptions(FeedOptions options, string partitionKey = null)
        {
            if (options == null)
            {
                options = new FeedOptions
                {
                    MaxItemCount = -1,
                  //  EnableCrossPartitionQuery = true,
                //    PartitionKey = new PartitionKey(partitionKey)
                };
            }
            return options;
        }

        public IQueryable<T> GetDocuments(FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("GetDocuments"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var query = _client.CreateDocumentQuery<T>(_collectionUri, $"SELECT * FROM {_collectionName}", feedOptions);

                return query;
            }
        }


        public T GetDocumentById(string documentId, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("GetDocumentById"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var query = _client.CreateDocumentQuery<T>(_collectionUri, $"SELECT TOP 1 * FROM {_collectionName} c WHERE c.id = '{documentId}'", feedOptions).AsEnumerable();

                return query.FirstOrDefault();
            }
        }


        public IQueryable<T> QueryByFilter(string filters, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("QueryByFilter"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var sql = $"SELECT * FROM {_collectionName} c WHERE {filters}";
                var query = _client.CreateDocumentQuery<T>(_collectionUri, sql, feedOptions);


                return query;
            }
        }


        public IQueryable<T> QueryBySql(string sqlQuery, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("QueryBySql"))
            {
                var feedOptions = EnsureFeedOptions(options);
         
                var query = _client.CreateDocumentQuery<T>(_collectionUri, sqlQuery, feedOptions);

                return query;
            }
        }


        public IEnumerable<T> QueryBy(Func<T, bool> whereCondition, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("QueryBy"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var query = _client
                            .CreateDocumentQuery<T>(_collectionUri, feedOptions)
                            .Where(whereCondition);

                return query;
            }
        }


        public long Count(FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("Count"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var query = _client.CreateDocumentQuery(_collectionUri, $"SELECT VALUE Count(1) FROM {_collectionName}", feedOptions).AsEnumerable();

                return query.FirstOrDefault();
            }
        }

        public long GetCountByFilter(string filters, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("GetCountByFilter"))
            {
                var feedOptions = EnsureFeedOptions(options);

                var query = _client.CreateDocumentQuery(_collectionUri, $"SELECT VALUE Count(1) FROM {_collectionName} c WHERE {filters}", feedOptions).AsEnumerable();

                return query.FirstOrDefault();
            }
        }


        public long Count(Func<T, bool> whereCondition, FeedOptions options = null)
        {
            using (MiniProfiler.Current.Step("Count"))
            {
                var feedOptions = EnsureFeedOptions(options);
                var result = _client
                              .CreateDocumentQuery<T>(_collectionUri, feedOptions)
                              .Where(whereCondition).Count();

                return result;
            }
        }


        public async Task<Document> CreateDocument(T document, RequestOptions options = null, bool disableAutomaticIdGeneration = false)
        {
            using (MiniProfiler.Current.Step("CreateDocument"))
            {
                return await _client.CreateDocumentAsync(_collectionUri, document, options: options, disableAutomaticIdGeneration: disableAutomaticIdGeneration);
            }
        }


        public async Task<Document> UpdateDocument(string documentId, T document, RequestOptions options = null)
        {
            using (MiniProfiler.Current.Step("UpdateDocument"))
            {
                var doc = document as dynamic;
                var docId = (string)doc.id; // must be lowercase!!
                var documentUri = UriFactory.CreateDocumentUri(_databaseName, _collectionName, docId);

                return await _client.ReplaceDocumentAsync(documentUri, document, options: options);
            }
        }


        public async Task<ResourceResponse<Document>> DeleteDocument(string documentId, RequestOptions options = null)
        {
            using (MiniProfiler.Current.Step("DeleteDocument"))
            {       
                var documentUri = UriFactory.CreateDocumentUri(_databaseName, _collectionName, documentId);
               // var res = await _client.DeleteDocumentAsync(documentLink, options: options);

                var res = await _client.DeleteDocumentAsync(documentUri, new RequestOptions()
                { PartitionKey = new Microsoft.Azure.Documents.PartitionKey(documentId) });

                return res;
            }
        }
    }
}
