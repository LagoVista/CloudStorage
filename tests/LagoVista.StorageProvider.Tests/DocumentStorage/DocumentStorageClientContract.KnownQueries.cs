using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.DocumentStorage
{
    internal static partial class DocumentStorageClientContract
    {
        public static async Task KnownEntityQueriesAsync(IDocumentStorageClient client)
        {
            var alpha = CreateEntity("Known Alpha", "Alpha Known");
            var bravo = CreateEntity("Known Bravo", "Bravo Known");
            await client.CreateDocumentAsync(alpha);
            await client.CreateDocumentAsync(bravo);

            var byTypeRequest = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentsByType)
                .WithParameter("entityType", nameof(ContractDocumentEntity))
                .WithParameter("orgId", "ORG1");

            var byType = (await client.QueryKnownAsync<JObject>(nameof(ContractDocumentEntity), byTypeRequest)).ToList();
            Assert.AreEqual(2, byType.Count);
            CollectionAssert.AreEquivalent(
                new[] { alpha.Id.Value, bravo.Id.Value },
                byType.Select(item => item.Value<string>("id")).ToArray());
            Assert.AreEqual("Alpha Known", byType[0].Value<string>("Name"));
            Assert.AreEqual("Bravo Known", byType[1].Value<string>("Name"));

            var byIdRequest = new DocumentQueryRequest(DocumentQueryType.EntityUtilsDocumentById)
                .WithParameter("entityType", nameof(ContractDocumentEntity))
                .WithParameter("orgId", "ORG1")
                .WithParameter("entityId", bravo.Id.Value);

            var byId = (await client.QueryKnownAsync<JObject>(nameof(ContractDocumentEntity), byIdRequest)).Single();
            Assert.AreEqual(bravo.Id.Value, byId.Value<string>("id"));
            Assert.AreEqual("Known Bravo", byId.Value<string>(nameof(ContractDocumentEntity.Detail)));

            var countRequest = new DocumentQueryRequest(DocumentQueryType.EntityUtilsCountByType)
                .WithParameter("entityType", nameof(ContractDocumentEntity))
                .WithParameter("orgId", "ORG1");

            var count = (await client.QueryKnownAsync<DocumentCountResult>(nameof(ContractDocumentEntity), countRequest)).Single();
            Assert.AreEqual(2, count.Count);
        }
    }
}
