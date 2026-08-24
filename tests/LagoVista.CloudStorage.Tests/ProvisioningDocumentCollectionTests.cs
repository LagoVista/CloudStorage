using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class ProvisioningDocumentCollectionTests
    {
        [Test]
        public async Task QueryAsync_EnsuresBeforeDelegating()
        {
            var sequence = new List<string>();
            var inner = new RecordingDocumentCollection(sequence);
            var collection = new ProvisioningDocumentCollection(inner, _ =>
            {
                sequence.Add("ensure");
                return Task.CompletedTask;
            });

            await collection.QueryAsync<TestDocument>(new DocumentQueryRequest(DocumentQueryType.EntityUtilsCountByType));

            Assert.That(sequence, Is.EqualTo(new[] { "ensure", "query" }));
        }

        private sealed class TestDocument { }

        private sealed class RecordingDocumentCollection : IDocumentCollection
        {
            private readonly List<string> _sequence;

            public RecordingDocumentCollection(List<string> sequence)
            {
                _sequence = sequence;
            }

            public Task<ListResponse<TDocument>> QueryAsync<TDocument>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, string>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class => throw new NotSupportedException();

            public Task<ListResponse<TProjection>> QueryAsync<TDocument, TProjection, TSort>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, Expression<Func<TDocument, TSort>> sort, ListRequest listRequest, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class => throw new NotSupportedException();

            public Task<IEnumerable<TProjection>> QueryAsync<TDocument, TProjection>(Expression<Func<TDocument, bool>> query, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default) where TDocument : class where TProjection : class => throw new NotSupportedException();

            public Task<IEnumerable<TResult>> QueryAsync<TResult>(DocumentQueryRequest request, CancellationToken cancellationToken = default) where TResult : class
            {
                _sequence.Add("query");
                return Task.FromResult<IEnumerable<TResult>>(Array.Empty<TResult>());
            }
        }
    }
}
