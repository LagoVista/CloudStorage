using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    internal static class DocumentStorageClientContract
    {
        public static Task DatabaseIdentityAsync(IDocumentStorageClient client, string expectedDatabaseName)
        {
            Assert.AreEqual(expectedDatabaseName, client.DatabaseName);
            return Task.CompletedTask;
        }

        public static async Task CrudLifecycleAsync(IDocumentStorageClient client, string partitionKey)
        {
            var entity = CreateEntity("Original");

            var create = await client.CreateDocumentAsync(entity);
            Assert.IsNotNull(create);
            Assert.IsNotNull(create.Resource);
            Assert.AreEqual(entity.Id, create.Resource.Id);
            Assert.IsFalse(String.IsNullOrWhiteSpace(entity.ETag));

            var createdETag = entity.ETag;
            var loaded = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Original", loaded.Detail);
            Assert.AreEqual(createdETag, loaded.ETag);

            var loadedByPartition = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id, partitionKey);
            Assert.IsNotNull(loadedByPartition);
            Assert.AreEqual(entity.Id, loadedByPartition.Id);

            entity.Detail = "Updated";
            var upsert = await client.UpsertDocumentAsync(entity);
            Assert.IsNotNull(upsert);
            Assert.IsNotNull(upsert.Resource);
            Assert.AreEqual("Updated", upsert.Resource.Detail);
            Assert.IsFalse(String.IsNullOrWhiteSpace(entity.ETag));
            Assert.AreNotEqual(createdETag, entity.ETag);

            var reloaded = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id);
            Assert.AreEqual("Updated", reloaded.Detail);
            Assert.AreEqual(entity.ETag, reloaded.ETag);

            var deleted = await client.DeleteDocumentAsync<ContractDocumentEntity>(entity.Id, partitionKey);
            Assert.IsNotNull(deleted);
            Assert.IsNotNull(deleted.Resource);
            Assert.AreEqual(entity.Id, deleted.Resource.Id);

            var missing = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id, throwOnNotFound: false);
            Assert.IsNull(missing);
        }

        public static async Task NotFoundSemanticsAsync(IDocumentStorageClient client, string partitionKey)
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();

            var missing = await client.GetDocumentAsync<ContractDocumentEntity>(id, throwOnNotFound: false);
            Assert.IsNull(missing);

            await Assert.ThrowsExactlyAsync<RecordNotFoundException>(
                () => client.GetDocumentAsync<ContractDocumentEntity>(id));

            await Assert.ThrowsExactlyAsync<RecordNotFoundException>(
                () => client.DeleteDocumentAsync<ContractDocumentEntity>(id, partitionKey));
        }

        public static async Task QueryAndPagingAsync(IDocumentStorageClient client)
        {
            var alpha = CreateEntity("match", "Alpha");
            var beta = CreateEntity("other", "Beta");
            var charlie = CreateEntity("match", "Charlie");

            await client.CreateDocumentAsync(alpha);
            await client.CreateDocumentAsync(beta);
            await client.CreateDocumentAsync(charlie);

            var matching = (await client.QueryAsync<ContractDocumentEntity>(item => item.Detail == "match")).ToList();
            Assert.AreEqual(2, matching.Count);
            CollectionAssert.AreEquivalent(new[] { alpha.Id.Value, charlie.Id.Value }, matching.Select(item => item.Id.Value).ToArray());

            var request = new ListRequest { PageIndex = 1, PageSize = 2 };
            var page = await client.QueryAsync<ContractDocumentEntity>(item => item.Detail != "missing", item => item.Name, request);

            Assert.IsNotNull(page);
            Assert.IsNotNull(page.Model);
            Assert.AreEqual(2, page.Model.Count());
            CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, page.Model.Select(item => item.Name).ToArray());

            request.PageIndex = 2;
            var secondPage = await client.QueryAsync<ContractDocumentEntity>(item => item.Detail != "missing", item => item.Name, request);
            Assert.AreEqual(1, secondPage.Model.Count());
            Assert.AreEqual("Charlie", secondPage.Model.Single().Name);
        }

        private static ContractDocumentEntity CreateEntity(string detail, string name = null)
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return new ContractDocumentEntity
            {
                Id = id,
                Key = $"contract-{id.ToLowerInvariant()}",
                Name = name ?? $"Contract {id}",
                EntityType = nameof(ContractDocumentEntity),
                OwnerOrganization = EntityHeader.Create("ORG1", "Contract Org"),
                Detail = detail
            };
        }
    }

    internal sealed class ContractDocumentEntity : EntityBase
    {
        public string Detail { get; set; }
    }
}
