using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    internal static partial class DocumentStorageClientContract
    {
        public static async Task ProjectionAndKeyLookupAsync(IDocumentStorageClient client)
        {
            var entity = CreateEntity("Projection Detail", "Projection Entity");
            await client.CreateDocumentAsync(entity);

            var projection = await client.GetDocumentProjectionAsync<JObject>(nameof(ContractDocumentEntity), entity.Id.Value);
            Assert.IsNotNull(projection);
            Assert.AreEqual(entity.Id.Value, projection.Value<string>("id"));
            Assert.AreEqual("Projection Detail", projection.Value<string>(nameof(ContractDocumentEntity.Detail)));

            var projections = (await client.GetDocumentProjectionsAsync<ContractDocumentProjection>(
                nameof(ContractDocumentEntity),
                item => item.Detail == "Projection Detail")).ToList();

            Assert.AreEqual(1, projections.Count);
            Assert.AreEqual(entity.Id.Value, projections[0].Id);
            Assert.AreEqual(entity.Key, projections[0].Key);

            var byKey = await client.GetDocumentProjectionByKeyAsync<JObject>(
                nameof(ContractDocumentEntity),
                entity.Key,
                "ORG1");

            Assert.IsNotNull(byKey);
            Assert.AreEqual(entity.Id.Value, byKey.Value<string>("id"));
            Assert.AreEqual(entity.Key, byKey.Value<string>("Key"));
        }

        public static async Task OwnedLookupAsync(IDocumentStorageClient client)
        {
            var entity = CreateEntity("Owned Detail", "Owned Entity");
            await client.CreateDocumentAsync(entity);

            var owned = await client.GetOwnedDocumentProjectionAsync<JObject>(entity.Id.Value, "ORG1", throwOnNotFound: false);
            Assert.IsNotNull(owned, "The provider must be able to resolve an owned document written through the normal typed document path.");
            Assert.AreEqual(entity.Id.Value, owned.Value<string>("id"));

            var wrongOwner = await client.GetOwnedDocumentProjectionAsync<JObject>(entity.Id.Value, "OTHERORG", throwOnNotFound: false);
            Assert.IsNull(wrongOwner);
        }

        public static async Task RawDocumentAndPageAsync(IDocumentStorageClient client)
        {
            const string entityType = nameof(ContractDocumentEntity);
            const string firstId = "00000000000000000000000000000001";
            const string secondId = "00000000000000000000000000000002";

            var firstJson = new JObject
            {
                ["id"] = firstId,
                ["EntityType"] = entityType,
                ["Key"] = "raw-first",
                ["Name"] = "Raw First",
                ["OwnerOrganization"] = new JObject { ["Id"] = "ORG1", ["Text"] = "Contract Org" },
                ["Detail"] = "Raw Original"
            }.ToString();

            var secondJson = new JObject
            {
                ["id"] = secondId,
                ["EntityType"] = entityType,
                ["Key"] = "raw-second",
                ["Name"] = "Raw Second",
                ["OwnerOrganization"] = new JObject { ["Id"] = "ORG1", ["Text"] = "Contract Org" },
                ["Detail"] = "Raw Second"
            }.ToString();

            var firstWrite = await client.UpsertRawDocumentAsync(entityType, firstId, firstJson);
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstWrite.ETag));
            Assert.AreEqual(201, firstWrite.StatusCode);

            await client.UpsertRawDocumentAsync(entityType, secondId, secondJson);

            var firstPage = await client.GetDocumentPageAsync<JObject>(entityType, pageSize: 1);
            Assert.AreEqual(1, firstPage.Items.Count);
            Assert.AreEqual(firstId, firstPage.Items.Single().Value<string>("id"));
            Assert.AreEqual(firstId, firstPage.ContinuationToken);

            var secondPage = await client.GetDocumentPageAsync<JObject>(entityType, firstPage.ContinuationToken, pageSize: 1);
            Assert.AreEqual(1, secondPage.Items.Count);
            Assert.AreEqual(secondId, secondPage.Items.Single().Value<string>("id"));

            var updatedJson = JObject.Parse(firstJson);
            updatedJson["Detail"] = "Raw Updated";
            var secondWrite = await client.UpsertRawDocumentAsync(entityType, firstId, updatedJson.ToString(), firstWrite.ETag);
            Assert.AreNotEqual(firstWrite.ETag, secondWrite.ETag);
            Assert.AreEqual(200, secondWrite.StatusCode);

            await Assert.ThrowsExactlyAsync<ContentModifiedException>(
                () => client.UpsertRawDocumentAsync(entityType, firstId, updatedJson.ToString(), firstWrite.ETag));

            var updated = await client.GetDocumentProjectionAsync<JObject>(entityType, firstId);
            Assert.AreEqual("Raw Updated", updated.Value<string>("Detail"));
            Assert.AreEqual(secondWrite.ETag, updated.Value<string>("ETag"));

            await client.DeleteDocumentAsync(entityType, firstId);
            var deleted = await client.GetDocumentProjectionAsync<JObject>(entityType, firstId, throwOnNotFound: false);
            Assert.IsNull(deleted);
        }
    }

    internal sealed class ContractDocumentProjection
    {
        public string Id { get; set; }
        public string EntityType { get; set; }
        public string Key { get; set; }
        public string Detail { get; set; }
    }
}
