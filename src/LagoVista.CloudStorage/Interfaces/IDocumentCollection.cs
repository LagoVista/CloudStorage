using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Models.UIMetaData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IDocumentCollection
    {
        Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class;
        Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class;
        Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class;

        /// <summary>Gets a raw document by its application-level id.</summary>
        Task<JObject> GetDocumentAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>Queries raw documents using the common provider-neutral equality-filter path.</summary>
        Task<IEnumerable<JObject>> QueryDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default);

        /// <summary>Counts raw documents using the common provider-neutral equality-filter path.</summary>
        Task<int> CountDocumentsAsync(DocumentFilterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes one explicitly registered query whose provider-specific implementation lives
        /// inside the Cosmos/Mongo adapter rather than in application repositories.
        /// </summary>
        Task<IEnumerable<TResult>> QueryKnownAsync<TResult>(KnownDocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class;
    }

    public interface IDocumentCollectionFactory
    {
        IDocumentCollection Create(string endpoint, string sharedKey, string databaseName, string collectionName = null);
        IDocumentCollection Create(DocumentStorageSettings settings, string collectionName = null);
        IDocumentCollection Create<TEntity>(string endpoint, string sharedKey, string databaseName, string collectionName = null) where TEntity : class;
        IDocumentCollection Create<TEntity>(DocumentStorageSettings settings, string collectionName = null) where TEntity : class;
    }
}
