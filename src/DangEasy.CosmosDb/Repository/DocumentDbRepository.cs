using DangEasy.CosmosDb.Interfaces;
using DangEasy.CosmosDb.Repository.Async;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace DangEasy.CosmosDb.Repository
{
    public class DocumentDbRepository<T> : IDocumentDbRepository<T> where T : class
    {
        const int MIN_THROUGHPUT = 400;

        protected readonly DocumentClient _client;
        protected readonly string _databaseName;

        protected readonly AsyncLazy<Database> _database;
        protected AsyncLazy<DocumentCollection> _collection;


        public DocumentDbRepository(DocumentClient client, string databaseName, string collectionName = null, RequestOptions options = null)
        {
            _client = client;
            _databaseName = databaseName;

            //if (options == null)
            //{
            //options = new RequestOptions
            //{
            //    OfferThroughput = MIN_THROUGHPUT,  // Because I'm cheap!
            //    PartitionKey = new PartitionKey("/id")
            //};
            //}

            _database = new AsyncLazy<Database>(async () => await GetOrCreateDatabaseAsync(options));
            _collection = new AsyncLazy<DocumentCollection>(async () => await GetOrCreateCollectionAsync(collectionName ?? typeof(T).Name));
        }


        public async Task<T> CreateAsync(T entity, RequestOptions requestOptions = null)
        {
            var createdDoc = await _client.CreateDocumentAsync((await _collection).SelfLink, entity, requestOptions);
            var createdEntity = JsonConvert.DeserializeObject<T>(createdDoc.Resource.ToString());

            return createdEntity;
        }

        public async Task<T> UpdateAsync(T entity, RequestOptions requestOptions = null)
        {
            var documentUri = UriFactory.CreateDocumentUri(_databaseName, (await _collection).Id, (string)GetId(entity));

            var updatedDoc = await _client.ReplaceDocumentAsync(documentUri, entity, requestOptions);

            var updatedEntity = JsonConvert.DeserializeObject<T>(updatedDoc.Resource.ToString());

            return updatedEntity;
        }

        public async Task<bool> DeleteAsync(string id, RequestOptions requestOptions = null)
        {
            var docUri = UriFactory.CreateDocumentUri((await _database).Id, (await _collection).Id, id);
            var result = await _client.DeleteDocumentAsync(docUri, requestOptions);

            var isSuccess = result.StatusCode == HttpStatusCode.NoContent;

            return isSuccess;
        }

        public async Task<int> CountAsync()
        {
            return _client.CreateDocumentQuery<T>((await _collection).SelfLink).Count();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return _client.CreateDocumentQuery<T>((await _collection).SelfLink).Where(predicate).Count();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return _client.CreateDocumentQuery<T>((await _collection).SelfLink).AsEnumerable();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            var retVal = await GetDocumentByIdAsync(id);
            return (T)(dynamic)retVal;
        }

        private async Task<Document> GetDocumentByIdAsync(object id)
        {
            return _client.CreateDocumentQuery<Document>((await _collection).SelfLink).Where(d => d.Id == id.ToString()).AsEnumerable().FirstOrDefault();
        }

        public async Task<T> FirstOrDefaultAsync(Func<T, bool> predicate)
        {
            return
                _client.CreateDocumentQuery<T>((await _collection).DocumentsLink)
                    .Where(predicate)
                    .AsEnumerable()
                    .FirstOrDefault();
        }

        public async Task<IQueryable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            return _client.CreateDocumentQuery<T>((await _collection).DocumentsLink)
                .Where(predicate);
        }

        public async Task<IQueryable<T>> QueryAsync()
        {
            return _client.CreateDocumentQuery<T>((await _collection).DocumentsLink);
        }




        //--
        // My custom queries
        //--

        public async Task<IQueryable<T>> QueryBySql(string sqlQuery, FeedOptions options = null)
        {
            var query = _client.CreateDocumentQuery<T>((await _collection).SelfLink, sqlQuery, options);

            return query;
        }

        //-- end my custom queries



        #region DB init 
        //--
        // DB Init / Creation helpers
        //--
        private async Task<DocumentCollection> GetOrCreateCollectionAsync(string collectionName)
        {
            DocumentCollection collection = _client.CreateDocumentCollectionQuery((await _database).SelfLink).Where(c => c.Id == collectionName).ToArray().FirstOrDefault();

            if (collection == null)
            {
                collection = new DocumentCollection
                {
                    Id = collectionName,
                    //IndexingPolicy = new IndexingPolicy { IndexingMode = IndexingMode.Consistent },
                    //PartitionKey = new PartitionKeyDefinition
                    //{
                    //    Paths = new Collection<string> { "/id" }
                    //},
                };

                collection = await _client.CreateDocumentCollectionAsync((await _database).SelfLink, collection);
            }

            return collection;
        }

        private async Task<Database> GetOrCreateDatabaseAsync(RequestOptions options = null)
        {
            Database database = _client.CreateDatabaseQuery().Where(db => db.Id == _databaseName).ToArray().FirstOrDefault();
            if (database == null)
            {
                database = await _client.CreateDatabaseAsync(new Database { Id = _databaseName }, options);
            }

            return database;
        }

        #endregion



        #region Reflection
        private object GetId(T entity)
        {
            var p = Expression.Parameter(typeof(T), "x");
            Expression body = Expression.Property(p, "id"); // lowercase in CosmosDb
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }
            var exp = Expression.Lambda<Func<T, object>>(body, p);
            return exp.Compile()(entity);
        }

        #endregion
    }
}
