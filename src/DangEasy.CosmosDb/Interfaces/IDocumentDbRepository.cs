using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DangEasy.CosmosDb.Interfaces
{
    public interface IDocumentDbRepository<T> where T : class
    {
        Task<T> CreateAsync(T entity, RequestOptions requestOptions = null);
        Task<T> UpdateAsync(T entity, RequestOptions requestOptions = null);
        Task<bool> DeleteAsync(string id, RequestOptions requestOptions = null);

        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<T> FirstOrDefaultAsync(Func<T, bool> predicate);
        Task<IQueryable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        Task<IQueryable<T>> QueryAsync();

        Task<IQueryable<T>> QueryBySql(string sqlQuery, FeedOptions options = null);
    }
}
