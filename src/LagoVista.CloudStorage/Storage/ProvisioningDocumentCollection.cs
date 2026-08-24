using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    internal sealed class ProvisioningDocumentCollection : IDocumentCollection
    {
        private readonly IDocumentCollection _inner;
        private readonly Func<CancellationToken, Task> _ensureExists;

        public ProvisioningDocumentCollection(IDocumentCollection inner, Func<CancellationToken, Task> ensureExists)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _ensureExists = ensureExists ?? throw new ArgumentNullException(nameof(ensureExists));
        }

        public async Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class
        {
            await _ensureExists(cancellationToken).ConfigureAwait(false);
            return await _inner.QueryAsync(query, sort, listRequest, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            await _ensureExists(cancellationToken).ConfigureAwait(false);
            return await _inner.QueryAsync(query, projection, sort, listRequest, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class
        {
            await _ensureExists(cancellationToken).ConfigureAwait(false);
            return await _inner.QueryAsync(query, projection, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<TResult>> QueryAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
        {
            await _ensureExists(cancellationToken).ConfigureAwait(false);
            return await _inner.QueryAsync<TResult>(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
