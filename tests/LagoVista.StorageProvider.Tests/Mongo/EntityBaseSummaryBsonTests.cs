using LagoVista.CloudStorage.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public sealed class EntityBaseSummaryBsonTests
    {
        [TestMethod]
        public void EntityBaseSummary_DeserializesFullDocumentWithStoredIdAndUnknownFields()
        {
            // Candidate repositories read complete entities into a summary DTO.
            // Legacy StoredId and unrelated entity-specific fields must not break the read.
            var document = new BsonDocument
            {
                { "_id", "ENTITY-1" },
                { "StoredId", "LEGACY-IDENTIFIER" },
                { "EntityType", "Campaign" },
                { "Name", "Example Campaign" },
                { "Key", "example-campaign" },
                { "FutureField", "Extra stored data" }
            };

            var summary = BsonSerializer.Deserialize<EntityBaseSummary>(document);

            Assert.AreEqual("ENTITY-1", summary.Id);
            Assert.AreEqual("Campaign", summary.EntityType);
            Assert.AreEqual("Example Campaign", summary.Name);
            Assert.AreEqual("example-campaign", summary.Key);
        }
    }
}
