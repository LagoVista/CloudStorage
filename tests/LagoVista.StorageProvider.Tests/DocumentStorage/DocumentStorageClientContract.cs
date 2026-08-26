using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        private static ContractDocumentEntity CreateEntity(string detail)
        {
            var id = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return new ContractDocumentEntity
            {
                Id = id,
                Key = $"contract-{id.ToLowerInvariant()}",
                Name = $"Contract {id}",
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
