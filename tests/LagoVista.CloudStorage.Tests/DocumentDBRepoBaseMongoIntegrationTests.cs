using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("Mongo")]
    public class DocumentDBRepoBaseMongoIntegrationTests
    {
        private string _connectionString;
        private string _databaseName;
        private string _logicalDatabaseName;
        private string _providerVariable;
        private string _mongoConnectionVariable;
        private string _mongoDatabaseVariable;
        private string _priorProvider;
        private string _priorMongoConnection;
        private string _priorMongoDatabase;
        private IMongoClient _client;
        private TestRepository _repository;

        [OneTimeSetUp]
        public void Setup()
        {
            _connectionString = GetTestConnectionString();
            _logicalDatabaseName = $"RepoBaseTest{Guid.NewGuid():N}";
            _databaseName = $"CloudStorageRepoBaseTests_{Guid.NewGuid():N}";
            var normalizedLogicalDatabaseName = _logicalDatabaseName.ToUpperInvariant();
            _providerVariable = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + normalizedLogicalDatabaseName;
            _mongoConnectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + normalizedLogicalDatabaseName;
            _mongoDatabaseVariable = DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariablePrefix + normalizedLogicalDatabaseName;

            _priorProvider = Environment.GetEnvironmentVariable(_providerVariable);
            _priorMongoConnection = Environment.GetEnvironmentVariable(_mongoConnectionVariable);
            _priorMongoDatabase = Environment.GetEnvironmentVariable(_mongoDatabaseVariable);

            Environment.SetEnvironmentVariable(_providerVariable, "mongo");
            Environment.SetEnvironmentVariable(_mongoConnectionVariable, _connectionString);
            Environment.SetEnvironmentVariable(_mongoDatabaseVariable, _databaseName);

            _client = new MongoClient(_connectionString);
            _repository = new TestRepository("https://cosmos.example:443/", null, _logicalDatabaseName);
        }

        [OneTimeTearDown]
        public async Task TearDownAsync()
        {
            try
            {
                if (_client != null && !String.IsNullOrWhiteSpace(_databaseName)) await _client.DropDatabaseAsync(_databaseName);
            }
            finally
            {
                if (!String.IsNullOrWhiteSpace(_providerVariable)) Environment.SetEnvironmentVariable(_providerVariable, _priorProvider);
                if (!String.IsNullOrWhiteSpace(_mongoConnectionVariable)) Environment.SetEnvironmentVariable(_mongoConnectionVariable, _priorMongoConnection);
                if (!String.IsNullOrWhiteSpace(_mongoDatabaseVariable)) Environment.SetEnvironmentVariable(_mongoDatabaseVariable, _priorMongoDatabase);
            }
        }

        [Test]
        public async Task RepositoryMongoPath_CreateGetUpdateQuerySoftDeleteAndHardDelete_WorksEndToEnd()
        {
            var entity = CreateEntity("ENTITY1", "Alpha", 1);

            var createResponse = await _repository.CreateAsync(entity);
            Assert.That(createResponse.Resource.Id, Is.EqualTo(entity.Id));
            Assert.That(_repository.GetCollectionName(), Is.EqualTo("RichMongoDomain"));
            Assert.That(_repository.GetPartitionKey(), Is.Null);

            var mongoCollection = _client.GetDatabase(_databaseName).GetCollection<TestEntity>("RichMongoDomain");
            var rawDocument = await mongoCollection.Find(item => item.Id == entity.Id).FirstOrDefaultAsync();
            Assert.That(rawDocument, Is.Not.Null);
            Assert.That(rawDocument.EntityType, Is.EqualTo(nameof(TestEntity)));

            var loaded = await _repository.GetAsync(entity.Id);
            Assert.That(loaded.Name, Is.EqualTo("Alpha"));

            loaded.Name = "Beta";
            loaded.Rank = 2;
            var updateResponse = await _repository.UpsertAsync(loaded);
            Assert.That(updateResponse.Resource.Name, Is.EqualTo("Beta"));
            Assert.That(updateResponse.Resource.Revision, Is.GreaterThan(0));

            await _repository.CreateAsync(CreateEntity("ENTITY2", "Gamma", 3));
            var listResponse = await _repository.QueryAsync(item => item.Rank >= 2, item => item.Name, new ListRequest { PageIndex = 1, PageSize = 10 });
            Assert.That(listResponse.Model.Select(item => item.Name), Is.EqualTo(new[] { "Beta", "Gamma" }));

            await _repository.DeleteAsync(entity.Id, true);
            var defaultList = await _repository.QueryAsync(item => item.Id == entity.Id, new ListRequest { PageIndex = 1, PageSize = 10 });
            Assert.That(defaultList.Model, Is.Empty);

            var deletedList = await _repository.QueryAsync(item => item.Id == entity.Id, new ListRequest { PageIndex = 1, PageSize = 10, ShowDeleted = true });
            Assert.That(deletedList.Model.Single().IsDeleted, Is.True);

            await _repository.DeleteAsync(entity.Id, false);
            Assert.That(await _repository.GetAsync(entity.Id, false), Is.Null);
        }

        [Test]
        public async Task RepositoryMongoPath_DatabaseSpecificProviderOverride_DoesNotRequireSharedKey()
        {
            _repository.SetConnection("https://cosmos.example:443/", null, _logicalDatabaseName);
            var entity = CreateEntity("SETCONNECTION", "Set Connection", 10);
            await _repository.CreateAsync(entity);
            Assert.That((await _repository.GetAsync(entity.Id)).Name, Is.EqualTo("Set Connection"));
        }

        private static string GetTestConnectionString()
        {
            try
            {
                return TestConnections.TestMongoDocumentStorage.BuildConnectionString();
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Configure TEST_MONGO_* settings or run run-mongo-tests.ps1. {ex.Message}");
                return null;
            }
        }

        private static TestEntity CreateEntity(string id, string name, int rank)
        {
            return new TestEntity
            {
                Id = id,
                Name = name,
                Rank = rank,
                OwnerOrganization = EntityHeader.Create("ORG1", "Organization One")
            };
        }

        private sealed class TestRepository : DocumentDBRepoBase<TestEntity>
        {
            public TestRepository(string endpoint, string sharedKey, string dbName) : base(endpoint, sharedKey, dbName, new AdminLogger(new ConsoleLogWriter()))
            {
            }

            public Task<OperationResponse<TestEntity>> CreateAsync(TestEntity entity)
            {
                return CreateDocumentAsync(entity);
            }

            public Task<TestEntity> GetAsync(string id, bool throwOnNotFound = true)
            {
                return GetDocumentAsync(id, throwOnNotFound);
            }

            public Task<OperationResponse<TestEntity>> UpsertAsync(TestEntity entity)
            {
                return UpsertDocumentAsync(entity);
            }

            public Task<OperationResponse<TestEntity>> DeleteAsync(string id, bool softDelete)
            {
                return DeleteDocumentAsync(id, softDelete);
            }

            public new Task<ListResponse<TestEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TestEntity, bool>> query, ListRequest listRequest)
            {
                return base.QueryAsync(query, listRequest);
            }

            public new Task<ListResponse<TestEntity>> QueryAsync(System.Linq.Expressions.Expression<Func<TestEntity, bool>> query, System.Linq.Expressions.Expression<Func<TestEntity, string>> sort, ListRequest listRequest)
            {
                return base.QueryAsync(query, sort, listRequest);
            }
        }

        [EntityDescription("RichMongoDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(DocumentDBRepoBaseMongoIntegrationTests))]
        private sealed class TestEntity : EntityBase
        {
            public int Rank { get; set; }
        }
    }
}
