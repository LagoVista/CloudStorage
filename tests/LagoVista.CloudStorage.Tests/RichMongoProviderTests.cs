using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using NUnit.Framework;

namespace LagoVista.CloudStorage.Tests
{
    public class RichMongoProviderTests
    {
        [Test]
        public void DocumentStorageFactory_WithMongoSettings_CreatesRichProviderUsingDomainCollection()
        {
            var settings = new DocumentStorageSettings
            {
                Provider = DocumentStorageProviderType.Mongo,
                DatabaseName = "LogicalDb",
                Mongo = new MongoDocumentStorageSettings
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "MongoTarget"
                }
            };

            var storage = DocumentStorageFactory.Create<RichMongoTestEntity>(settings, null);

            Assert.That(storage.GetCollectionName(), Is.EqualTo("RichMongoDomain"));
            Assert.That(storage.GetPartitionKey(), Is.Null);
        }

        [Test]
        public void OperationResponse_WithProviderNeutralResource_ReturnsResource()
        {
            var entity = new RichMongoTestEntity { Id = "ABC123", Name = "Test" };
            var response = new OperationResponse<RichMongoTestEntity>(entity);
            Assert.That(response.Resource, Is.SameAs(entity));
        }

        [EntityDescription("RichMongoDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(RichMongoProviderTests))]
        private sealed class RichMongoTestEntity : EntityBase
        {
        }
    }
}
