using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Microsoft.Azure.Cosmos;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("StorageParity")]
    public class EntityUtilsRepositoryStorageParityTests
    {
        private const string OrganizationId = "ORG-PARITY";
        private const string EntityType = nameof(ParityUtilsEntity);

        [Test]
        public Task GetEntitiesByTypeAsync_CosmosAndMongo_ReturnSameOrganizationEntitiesInNameOrder() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var cosmosResult = await cosmos.GetEntitiesByTypeAsync(EntityType, OrganizationId, CancellationToken.None);
                var mongoResult = await mongo.GetEntitiesByTypeAsync(EntityType, OrganizationId, CancellationToken.None);

                Assert.That(mongoResult.Successful, Is.True);
                Assert.That(cosmosResult.Successful, Is.True);
                Assert.That(ToRawKeys(mongoResult.Result), Is.EqualTo(ToRawKeys(cosmosResult.Result)));
                Assert.That(ToRawKeys(cosmosResult.Result), Is.EqualTo(new[]
                {
                    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA|Alpha",
                    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB|Beta"
                }));
            });

        [Test]
        public Task GetEntityCoreAsync_CosmosAndMongo_ReturnSameSummaries() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var org = EntityHeader.Create(OrganizationId, "Parity Organization");
                var cosmosResult = await cosmos.GetEntityCoreAsync(EntityType, org);
                var mongoResult = await mongo.GetEntityCoreAsync(EntityType, org);

                Assert.That(ToCoreKeys(mongoResult), Is.EqualTo(ToCoreKeys(cosmosResult)));
                Assert.That(ToCoreKeys(cosmosResult), Is.EqualTo(new[] { "Alpha|alpha|ALP", "Beta|beta|BET" }));
            });

        [Test]
        public Task GetEntityBasesAsync_CosmosAndMongo_ReturnSameSummaries() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var org = EntityHeader.Create(OrganizationId, "Parity Organization");
                var cosmosResult = await cosmos.GetEntityBasesAsync(EntityType, org);
                var mongoResult = await mongo.GetEntityBasesAsync(EntityType, org);

                Assert.That(ToBaseKeys(mongoResult), Is.EqualTo(ToBaseKeys(cosmosResult)));
                Assert.That(ToBaseKeys(cosmosResult), Is.EqualTo(new[] { "Alpha|alpha", "Beta|beta" }));
            });

        [Test]
        public Task GetEntityByIdAsync_CosmosAndMongo_EnforceEntityTypeAndOrganization() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                const string id = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                var cosmosFound = await cosmos.GetEntityByIdAsync(EntityType, id, OrganizationId, CancellationToken.None);
                var mongoFound = await mongo.GetEntityByIdAsync(EntityType, id, OrganizationId, CancellationToken.None);
                var cosmosWrongOrg = await cosmos.GetEntityByIdAsync(EntityType, id, "ORG-OTHER", CancellationToken.None);
                var mongoWrongOrg = await mongo.GetEntityByIdAsync(EntityType, id, "ORG-OTHER", CancellationToken.None);
                var cosmosWrongType = await cosmos.GetEntityByIdAsync("OtherEntityType", id, OrganizationId, CancellationToken.None);
                var mongoWrongType = await mongo.GetEntityByIdAsync("OtherEntityType", id, OrganizationId, CancellationToken.None);

                Assert.That((string)mongoFound?["id"], Is.EqualTo((string)cosmosFound?["id"]));
                Assert.That((string)cosmosFound?["Name"], Is.EqualTo("Alpha"));
                Assert.That(cosmosWrongOrg, Is.Null);
                Assert.That(mongoWrongOrg, Is.Null);
                Assert.That(cosmosWrongType, Is.Null);
                Assert.That(mongoWrongType, Is.Null);
            });

        [Test]
        public Task CountEntitiesByTypeAsync_CosmosAndMongo_ReturnSameOrganizationCount() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var cosmosResult = await cosmos.CountEntitiesByTypeAsync(EntityType, OrganizationId, CancellationToken.None);
                var mongoResult = await mongo.CountEntitiesByTypeAsync(EntityType, OrganizationId, CancellationToken.None);

                Assert.That(mongoResult.Successful, Is.True);
                Assert.That(cosmosResult.Successful, Is.True);
                Assert.That(mongoResult.Result, Is.EqualTo(cosmosResult.Result));
                Assert.That(cosmosResult.Result, Is.EqualTo(2));
            });

        private static async Task WithRepositoriesAsync(Func<IEntityUtilsRepository, IEntityUtilsRepository, Task> assertion)
        {
            var cosmosDatabaseName = $"UtilsParityCosmos_{Guid.NewGuid():N}";
            var mongoLogicalDatabaseName = $"UtilsParityMongo{Guid.NewGuid():N}";
            var mongoDatabaseName = $"UtilsParityMongo_{Guid.NewGuid():N}";
            var mongoConnectionString = TestConnections.TestMongoDocumentStorage.BuildConnectionString();
            var normalizedLogicalName = mongoLogicalDatabaseName.ToUpperInvariant();
            var providerVariable = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + normalizedLogicalName;
            var connectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + normalizedLogicalName;
            var databaseVariable = DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariablePrefix + normalizedLogicalName;
            var priorProvider = Environment.GetEnvironmentVariable(providerVariable);
            var priorConnection = Environment.GetEnvironmentVariable(connectionVariable);
            var priorDatabase = Environment.GetEnvironmentVariable(databaseVariable);

            using var cosmosProvider = new CosmosClientProvider();
            var cosmosClient = cosmosProvider.GetClient(StorageLabConnections.CosmosEndpoint, StorageLabConnections.CosmosKey);
            var mongoClient = new MongoClient(mongoConnectionString);
            Database cosmosDatabase = null;

            try
            {
                Environment.SetEnvironmentVariable(providerVariable, "mongo");
                Environment.SetEnvironmentVariable(connectionVariable, mongoConnectionString);
                Environment.SetEnvironmentVariable(databaseVariable, mongoDatabaseName);

                cosmosDatabase = (await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDatabaseName)).Database;
                var cosmosContainer = (await cosmosDatabase.CreateContainerIfNotExistsAsync($"{cosmosDatabaseName}_Collections", "/EntityType")).Container;
                var mongoCollection = mongoClient.GetDatabase(mongoDatabaseName).GetCollection<BsonDocument>("EntityUtilsParityDomain");

                var documents = new[]
                {
                    CreateDocument("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Alpha", "alpha", "ALP", OrganizationId),
                    CreateDocument("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", "Beta", "beta", "BET", OrganizationId),
                    CreateDocument("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", "Other Org", "other", "OTH", "ORG-OTHER")
                };

                foreach (var document in documents)
                {
                    await cosmosContainer.CreateItemAsync(document.Cosmos, new PartitionKey(EntityType));
                    await mongoCollection.InsertOneAsync(document.Mongo);
                }

                var logger = new AdminLogger(new ConsoleLogWriter());
                var entityDetailResponseFactory = Stub<IEntityDetailResponseFactory>();
                var dependencyManager = Stub<IDependencyManager>();
                var cacheProvider = Stub<ICacheProvider>();
                var ragIndexingServices = Stub<IRagIndexingServices>();
                var entityListCacheInvalidator = Stub<IEntityListCacheInvalidator>();

                IEntityUtilsRepository cosmosRepository = new ProviderNeutralEntityUtilsRepository(
                    new TestSyncConnectionSettings(new ConnectionSettings
                    {
                        Uri = StorageLabConnections.CosmosEndpoint,
                        AccessKey = StorageLabConnections.CosmosKey,
                        ResourceName = cosmosDatabaseName
                    }),
                    cosmosProvider,
                    entityDetailResponseFactory,
                    dependencyManager,
                    cacheProvider,
                    logger,
                    ragIndexingServices,
                    entityListCacheInvalidator);

                IEntityUtilsRepository mongoRepository = new ProviderNeutralEntityUtilsRepository(
                    new TestSyncConnectionSettings(new ConnectionSettings
                    {
                        Uri = StorageLabConnections.CosmosEndpoint,
                        AccessKey = StorageLabConnections.CosmosKey,
                        ResourceName = mongoLogicalDatabaseName
                    }),
                    cosmosProvider,
                    entityDetailResponseFactory,
                    dependencyManager,
                    cacheProvider,
                    logger,
                    ragIndexingServices,
                    entityListCacheInvalidator);

                await assertion(cosmosRepository, mongoRepository);
            }
            finally
            {
                if (cosmosDatabase != null) await cosmosDatabase.DeleteAsync();
                await mongoClient.DropDatabaseAsync(mongoDatabaseName);
                Environment.SetEnvironmentVariable(providerVariable, priorProvider);
                Environment.SetEnvironmentVariable(connectionVariable, priorConnection);
                Environment.SetEnvironmentVariable(databaseVariable, priorDatabase);
            }
        }

        private static string[] ToRawKeys(IEnumerable<Newtonsoft.Json.Linq.JObject> documents) =>
            documents.Select(doc => $"{(string)doc["id"]}|{(string)doc["Name"]}").ToArray();

        private static string[] ToCoreKeys(IEnumerable<EntityCoreSummary> entities) =>
            entities.Select(entity => $"{entity.Name}|{entity.Key}|{entity.Tla}").ToArray();

        private static string[] ToBaseKeys(IEnumerable<EntityBaseSummary> entities) =>
            entities.Select(entity => $"{entity.Name}|{entity.Key}").ToArray();

        private static (Dictionary<string, object> Cosmos, BsonDocument Mongo) CreateDocument(string id, string name, string key, string tla, string orgId)
        {
            var cosmos = new Dictionary<string, object>
            {
                ["id"] = id,
                ["EntityType"] = EntityType,
                ["Name"] = name,
                ["Key"] = key,
                ["Tla"] = tla,
                ["Description"] = $"{name} description",
                ["OwnerOrganization"] = new Dictionary<string, object> { ["Id"] = orgId, ["Text"] = orgId },
                ["IsDraft"] = false,
                ["IsDeprecated"] = false,
                ["Revision"] = 1
            };

            var mongo = new BsonDocument
            {
                { "_id", id },
                { "EntityType", EntityType },
                { "Name", name },
                { "Key", key },
                { "Tla", tla },
                { "Description", $"{name} description" },
                { "OwnerOrganization", new BsonDocument { { "Id", orgId }, { "Text", orgId } } },
                { "IsDraft", false },
                { "IsDeprecated", false },
                { "Revision", 1 }
            };

            return (cosmos, mongo);
        }

        private static T Stub<T>() where T : class => DispatchProxy.Create<T, NullDispatchProxy>();

        private sealed class NullDispatchProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var returnType = targetMethod.ReturnType;
                if (returnType == typeof(void)) return null;
                if (returnType.IsValueType) return Activator.CreateInstance(returnType);
                return null;
            }
        }

        private sealed class TestSyncConnectionSettings : ISyncConnectionSettings
        {
            public TestSyncConnectionSettings(IConnectionSettings settings) => SyncConnectionSettings = settings;
            public IConnectionSettings SyncConnectionSettings { get; }
        }

        [EntityDescription("EntityUtilsParityDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(EntityUtilsRepositoryStorageParityTests))]
        private sealed class ParityUtilsEntity { }
    }
}
