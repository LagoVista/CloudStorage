using LagoVista.CloudStorage.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public sealed class EntityCoreSummaryBsonTests
    {
        [TestMethod]
        public void EntityCoreSummary_DeserializesFullDocumentWithStoredId()
        {
            // GetEntityCoreAsync reads complete entity documents into a small read-only summary.
            var document = new BsonDocument
            {
                { "_id", "ENTITY-1" },
                { "StoredId", "LEGACY-IDENTIFIER" },
                { "EntityType", "TeamMember" },
                { "OwnerOrganization", new BsonDocument { { "Id", "ORG1" } } },
                { "Name", "Example Team Member" },
                { "Key", "example-team-member" },
                { "PurposeSummary", "Assists customers" },
                { "FutureField", "Extra stored data" }
            };

            var summary = BsonSerializer.Deserialize<EntityCoreSummary>(document);

            Assert.AreEqual("ENTITY-1", summary.Id);
            Assert.AreEqual("Example Team Member", summary.Name);
            Assert.AreEqual("example-team-member", summary.Key);
            Assert.AreEqual("Assists customers", summary.PurposeSummary);
        }
    }
}
