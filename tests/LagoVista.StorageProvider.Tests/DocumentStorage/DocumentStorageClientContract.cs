using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
            var alpha = CreateEntity("keep", "Alpha");
            var bravo = CreateEntity("skip", "Bravo");
            var charlie = CreateEntity("keep", "Charlie");

            await client.CreateDocumentAsync(charlie);
            await client.CreateDocumentAsync(alpha);
            await client.CreateDocumentAsync(bravo);

            var unpaged = (await client.QueryAsync<ContractDocumentEntity>(item => item.Detail == "keep")).ToList();
            Assert.AreEqual(2, unpaged.Count);
            CollectionAssert.AreEquivalent(new[] { alpha.Id.Value, charlie.Id.Value }, unpaged.Select(item => item.Id.Value).ToArray());

            var firstPage = await client.QueryAsync<ContractDocumentEntity>(
                item => item.Detail == "keep",
                item => item.Name,
                new ListRequest { PageIndex = 1, PageSize = 1 });

            Assert.AreEqual(1, firstPage.Model.Count());
            Assert.AreEqual("Alpha", firstPage.Model.Single().Name);

            var secondPage = await client.QueryAsync<ContractDocumentEntity>(
                item => item.Detail == "keep",
                item => item.Name,
                new ListRequest { PageIndex = 2, PageSize = 1 });

            Assert.AreEqual(1, secondPage.Model.Count());
            Assert.AreEqual("Charlie", secondPage.Model.Single().Name);
        }

        public static async Task OptimisticConcurrencyAsync(IDocumentStorageClient client)
        {
            var entity = CreateEntity("Original");
            await client.CreateDocumentAsync(entity);

            var originalETag = entity.ETag;
            entity.Detail = "Updated with valid ETag";
            await client.UpsertDocumentAsync(entity, originalETag);

            Assert.AreNotEqual(originalETag, entity.ETag);
            var currentETag = entity.ETag;

            entity.Detail = "Stale update";
            await Assert.ThrowsExactlyAsync<ContentModifiedException>(
                () => client.UpsertDocumentAsync(entity, originalETag));

            Assert.AreEqual(currentETag, entity.ETag);

            var reloaded = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id);
            Assert.AreEqual("Updated with valid ETag", reloaded.Detail);
            Assert.AreEqual(currentETag, reloaded.ETag);
        }

        public static async Task PatchAsync(IDocumentStorageClient client)
        {
            var entity = CreateEntity("Original");
            entity.OptionalDetail = "Remove Me";
            await client.CreateDocumentAsync(entity);

            var originalETag = entity.ETag;
            var request = new PatchRequest
            {
                Id = entity.Id.Value,
                EntityType = nameof(ContractDocumentEntity),
                ETag = originalETag,
                Steps = new[]
                {
                    new PatchStep { Op = PatchOp.Set, LogicalPath = nameof(ContractDocumentEntity.Detail), Value = JToken.FromObject("Patched") },
                    new PatchStep { Op = PatchOp.Remove, LogicalPath = nameof(ContractDocumentEntity.OptionalDetail) }
                }
            };

            var result = await client.PatchDocumentAsync<ContractDocumentEntity>(request);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Resource);
            Assert.AreEqual("Patched", result.Resource.Detail);
            Assert.IsNull(result.Resource.OptionalDetail);
            Assert.AreNotEqual(originalETag, result.Resource.ETag);

            var reloaded = await client.GetDocumentAsync<ContractDocumentEntity>(entity.Id);
            Assert.AreEqual("Patched", reloaded.Detail);
            Assert.IsNull(reloaded.OptionalDetail);
            Assert.AreEqual(result.Resource.ETag, reloaded.ETag);

            var staleRequest = new PatchRequest
            {
                Id = entity.Id.Value,
                EntityType = nameof(ContractDocumentEntity),
                ETag = originalETag,
                Steps = new[]
                {
                    new PatchStep { Op = PatchOp.Set, LogicalPath = nameof(ContractDocumentEntity.Detail), Value = JToken.FromObject("Should Fail") }
                }
            };

            await Assert.ThrowsExactlyAsync<ContentModifiedException>(
                () => client.PatchDocumentAsync<ContractDocumentEntity>(staleRequest));
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
        public string OptionalDetail { get; set; }
    }
}
