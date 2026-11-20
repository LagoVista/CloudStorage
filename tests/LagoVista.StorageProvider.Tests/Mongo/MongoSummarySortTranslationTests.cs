using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.StorageProvider.Tests.DocumentStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [TestCategory("Mongo")]
    public sealed class MongoSummarySortTranslationTests
    {
        [TestMethod]
        public void UtcTimestampImplicitStringConversion_SortsSerializedFieldDescending()
        {
            MongoBsonSerialization.Configure();

            // Mirrors DdrRepo's qry => qry.LastUpdatedDate expression.
            var sort = MongoDocumentStorageClient.CreateSummarySortDefinition<ContractSummaryEntity>(
                entity => entity.LastUpdatedDate, descending: true);

            var rendered = sort.Render(new RenderArgs<ContractSummaryEntity>(
                BsonSerializer.LookupSerializer<ContractSummaryEntity>(),
                BsonSerializer.SerializerRegistry));

            Assert.AreEqual(1, rendered.ElementCount);
            Assert.AreEqual(-1, rendered["LastUpdatedDate"].AsInt32);
        }

        [TestMethod]
        public void StringMemberSort_PreservesNormalAscendingField()
        {
            MongoBsonSerialization.Configure();

            var sort = MongoDocumentStorageClient.CreateSummarySortDefinition<ContractSummaryEntity>(
                entity => entity.Name, descending: false);

            var rendered = sort.Render(new RenderArgs<ContractSummaryEntity>(
                BsonSerializer.LookupSerializer<ContractSummaryEntity>(),
                BsonSerializer.SerializerRegistry));

            Assert.AreEqual(1, rendered.ElementCount);
            Assert.AreEqual(1, rendered["Name"].AsInt32);
        }
    }
}
