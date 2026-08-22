using LagoVista.Core.Models.UIMetaData;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCollection
    {
        Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class;
    }

    public interface IDocumentCollectionFactory
    {
        IDocumentCollection Create(string endpoint, string sharedKey, string databaseName, string collectionName = null);
    }
}
