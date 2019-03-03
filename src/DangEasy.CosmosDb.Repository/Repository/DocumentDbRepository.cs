using DangEasy.Interfaces.Database;
using DangEasy.CosmosDb.Repository.Async;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace DangEasy.CosmosDb.Repository
{
    public class DocumentDbRepository<TEntity> : IRepositoryExtended<TEntity>
        where TEntity : class
    {
        const int MIN_THROUGHPUT = 400;

        protected readonly DocumentClient _client;
        protected readonly string _databaseName;

        protected readonly AsyncLazy<Database> _database;
        protected AsyncLazy<DocumentCollection> _collection;
        protected RequestOptions _options;

        public DocumentDbRepository(DocumentClient client, string databaseName, string collectionName = null, RequestOptions options = null)
        {
            _client = client;
            _databaseName = databaseName;
            _options = options;
            //if (options == null)
            //{
            //options = new RequestOptions
            //{
            //    OfferThroughput = MIN_THROUGHPUT,  // Because I'm cheap!
            //    PartitionKey = new PartitionKey("/id")
            //};
            //}
            _database = new AsyncLazy<Database>(async () => await GetOrCreateDatabaseAsync(options));
            _collection = new AsyncLazy<DocumentCollection>(async () => await GetOrCreateCollectionAsync(collectionName ?? typeof(TEntity).Name));
        }


        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            var createdDoc = await _client.CreateDocumentAsync((await _collection).SelfLink, entity, _options);
            var createdEntity = JsonConvert.DeserializeObject<TEntity>(createdDoc.Resource.ToString());


            return createdEntity;
        }


        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            var documentUri = UriFactory.CreateDocumentUri(_databaseName, (await _collection).Id, (string)GetId(entity));

            var updatedDoc = await _client.ReplaceDocumentAsync(documentUri, entity, _options);

            var updatedEntity = JsonConvert.DeserializeObject<TEntity>(updatedDoc.Resource.ToString());

            return updatedEntity;
        }


        public async Task<bool> DeleteAsync(object id)
        {
            var docUri = UriFactory.CreateDocumentUri((await _database).Id, (await _collection).Id, id as string);
            var result = await _client.DeleteDocumentAsync(docUri, _options);

            var isSuccess = result.StatusCode == HttpStatusCode.NoContent;

            return isSuccess;
        }


        public async Task<int> CountAsync()
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).SelfLink).Count();
        }


        public async Task<int> CountAsync(string sqlQuery)
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).SelfLink, sqlQuery).Count();
        }


        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).SelfLink).Where(predicate).Count();
        }


        public async Task<IQueryable<TEntity>> GetAllAsync()
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).SelfLink);
        }


        public async Task<TEntity> GetByIdAsync(object id)
        {
            var retVal = await GetDocumentByIdAsync(id);
            return (TEntity)(dynamic)retVal;
        }


        private async Task<Document> GetDocumentByIdAsync(object id)
        {
            return _client.CreateDocumentQuery<Document>((await _collection).SelfLink).Where(d => d.Id == id.ToString()).AsEnumerable().FirstOrDefault();
        }


        public async Task<TEntity> FirstOrDefaultAsync(string sqlQuery)
        {
            return
               _client.CreateDocumentQuery<TEntity>((await _collection).DocumentsLink, sqlQuery)
                   .AsEnumerable()
                   .FirstOrDefault();
        }


        public async Task<TEntity> FirstOrDefaultAsync(Func<TEntity, bool> predicate)
        {
            return
                _client.CreateDocumentQuery<TEntity>((await _collection).DocumentsLink)
                    .Where(predicate)
                    .AsEnumerable()
                    .FirstOrDefault();
        }


        public async Task<IQueryable<TEntity>> QueryAsync(string sqlQuery)
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).DocumentsLink, sqlQuery);
        }


        public async Task<IQueryable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _client.CreateDocumentQuery<TEntity>((await _collection).DocumentsLink)
                .Where(predicate);
        }


        public async Task<TResult> ExecuteStoredProcedureAsync<TResult>(string sprocName, params object[] parameters)
        {
            var sprocUri = UriFactory.CreateStoredProcedureUri((await _database).Id, (await _collection).Id, sprocName);

            return await _client.ExecuteStoredProcedureAsync<TResult>(sprocUri, parameters);
        }





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

        private object GetId(TEntity entity)
        {
            var p = Expression.Parameter(typeof(TEntity), "x");
            Expression body = Expression.Property(p, "id"); // lowercase in CosmosDb
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }
            var exp = Expression.Lambda<Func<TEntity, object>>(body, p);
            return exp.Compile()(entity);
        }

        #endregion
    }
}
