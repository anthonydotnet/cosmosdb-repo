using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Dang.CosmosDb.Interfaces
{
    public interface ICosmosDbRepository<T> where T: class
    {
        IQueryable<T> GetDocuments(FeedOptions options = null);
        T GetDocumentById(string documentId, FeedOptions options = null);
        IQueryable<T> QueryByFilter(string filters, FeedOptions options = null);
        IQueryable<T> QueryBySql(string sqlQuery, FeedOptions options = null);
        IEnumerable<T> QueryBy(Func<T, bool> predicate, FeedOptions options = null);
        long Count(FeedOptions options = null);
        long GetCountByFilter(string filters, FeedOptions options = null);
        long Count(Func<T, bool> whereCondition, FeedOptions options = null);

        Task<Document> CreateDocument(T document, RequestOptions options = null, bool disableAutomaticIdGeneration = false);
        Task<Document> UpdateDocument(string documentId, T document, RequestOptions options = null);
        Task<ResourceResponse<Document>> DeleteDocument(string documentId, RequestOptions options = null);
    }
}