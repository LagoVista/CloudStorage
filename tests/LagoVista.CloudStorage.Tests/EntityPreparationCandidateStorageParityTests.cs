using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
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
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("StorageParity")]
    public class EntityPreparationCandidateStorageParityTests
    {
        private const string EntityType = nameof(ParityEntity);
        private const string OrganizationId = "ORG-PARITY";

        [Test]
        public async Task EntityPreparationCandidateRepository_CosmosAndMongo_ReturnEquivalentBusinessResults()
        {
            var cosmosDatabaseName = $"CloudStorageCandidateParityCosmos_{Guid.NewGuid():N}";
            var mongoLogicalDatabaseName = $"CandidateParityMongo{Guid.NewGuid():N}";
            var mongoDatabaseName = $"CloudStorageCandidateParityMongo_{Guid.NewGuid():N}";
            var mongoConnectionString = TestConnections.TestMongoDocumentStorage.BuildConnectionString();
            var normalizedMongoLogicalDatabaseName = mongoLogicalDatabaseName.ToUpperInvariant();
            var providerVariable = DocumentStorageSettingsResolver.ProviderEnvironmentVariablePrefix + normalizedMongoLogicalDatabaseName;
            var mongoConnectionVariable = DocumentStorageSettingsResolver.MongoConnectionStringEnvironmentVariablePrefix + normalizedMongoLogicalDatabaseName;
            var mongoDatabaseVariable = DocumentStorageSettingsResolver.MongoDatabaseEnvironmentVariablePrefix + normalizedMongoLogicalDatabaseName;
            var priorProvider = Environment.GetEnvironmentVariable(providerVariable);
            var priorMongoConnection = Environment.GetEnvironmentVariable(mongoConnectionVariable);
            var priorMongoDatabase = Environment.GetEnvironmentVariable(mongoDatabaseVariable);

            using var cosmosProvider = new CosmosClientProvider();
            var cosmosClient = cosmosProvider.GetClient(StorageLabConnections.CosmosEndpoint, StorageLabConnections.CosmosKey);
            var mongoClient = new MongoClient(mongoConnectionString);
            Database cosmosDatabase = null;

            try
            {
                Environment.SetEnvironmentVariable(providerVariable, "mongo");
                Environment.SetEnvironmentVariable(mongoConnectionVariable, mongoConnectionString);
                Environment.SetEnvironmentVariable(mongoDatabaseVariable, mongoDatabaseName);

                cosmosDatabase = (await cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDatabaseName)).Database;
                var cosmosContainer = (await cosmosDatabase.CreateContainerIfNotExistsAsync($"{cosmosDatabaseName}_Collections", "/EntityType")).Container;
                var mongoCollection = mongoClient.GetDatabase(mongoDatabaseName).GetCollection<BsonDocument>("ParityDomain");

                var documents = new[]
                {
                    CreateDocument("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Alpha", false, includeMasterStatus: true),
                    CreateDocument("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", "Beta", true, includeMasterStatus: true),
                    CreateDocument("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", "Charlie", false, includeMasterStatus: false),
                    CreateDocument("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD", "Other Org", false, includeMasterStatus: true, orgId: "ORG-OTHER")
                };

                foreach (var document in documents)
                {
                    await cosmosContainer.CreateItemAsync(document.Cosmos, new PartitionKey(EntityType));
                    await mongoCollection.InsertOneAsync(document.Mongo);
                }

                var logger = new AdminLogger(new ConsoleLogWriter());
                var cosmosRepository = new EntityPreparationCandidateRepository(
                    new TestSyncConnectionSettings(new ConnectionSettings
                    {
                        Uri = StorageLabConnections.CosmosEndpoint,
                        AccessKey = StorageLabConnections.CosmosKey,
                        ResourceName = cosmosDatabaseName
                    }),
                    cosmosProvider,
                    logger);

                var mongoRepository = new EntityPreparationCandidateRepository(
                    new TestSyncConnectionSettings(new ConnectionSettings
                    {
                        Uri = "https://cosmos-unused.example/",
                        AccessKey = null,
                        ResourceName = mongoLogicalDatabaseName
                    }),
                    cosmosProvider,
                    logger);

                var cosmosById = await cosmosRepository.GetEntityBaseAsync(EntityType, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", OrganizationId);
                var mongoById = await mongoRepository.GetEntityBaseAsync(EntityType, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", OrganizationId);
                Assert.That(ToKey(mongoById), Is.EqualTo(ToKey(cosmosById)));

                var cosmosAll = await cosmosRepository.GetEntityBasesAsync(EntityType, OrganizationId);
                var mongoAll = await mongoRepository.GetEntityBasesAsync(EntityType, OrganizationId);
                Assert.That(ToKeys(mongoAll), Is.EqualTo(ToKeys(cosmosAll)));
                Assert.That(ToKeys(cosmosAll), Is.EqualTo(new[]
                {
                    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA|Alpha",
                    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB|Beta",
                    "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC|Charlie"
                }));

                var cosmosIncomplete = await cosmosRepository.GetIncompleteEntityBasesAsync(EntityType, OrganizationId, 10);
                var mongoIncomplete = await mongoRepository.GetIncompleteEntityBasesAsync(EntityType, OrganizationId, 10);
                Assert.That(ToKeys(mongoIncomplete), Is.EqualTo(ToKeys(cosmosIncomplete)));
                Assert.That(ToKeys(cosmosIncomplete), Is.EqualTo(new[]
                {
                    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA|Alpha",
                    "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC|Charlie"
                }));

                var cosmosLimited = await cosmosRepository.GetIncompleteEntityBasesAsync(EntityType, OrganizationId, 1);
                var mongoLimited = await mongoRepository.GetIncompleteEntityBasesAsync(EntityType, OrganizationId, 1);
                Assert.That(ToKeys(mongoLimited), Is.EqualTo(ToKeys(cosmosLimited)));
                Assert.That(cosmosLimited.Count, Is.EqualTo(1));
                Assert.That(cosmosLimited[0].Name, Is.EqualTo("Alpha"));
            }
            finally
            {
                if (cosmosDatabase != null) await cosmosDatabase.DeleteAsync();
                await mongoClient.DropDatabaseAsync(mongoDatabaseName);
                Environment.SetEnvironmentVariable(providerVariable, priorProvider);
                Environment.SetEnvironmentVariable(mongoConnectionVariable, priorMongoConnection);
                Environment.SetEnvironmentVariable(mongoDatabaseVariable, priorMongoDatabase);
            }
        }

        private static string ToKey(EntityBaseSummary entity) => $"{entity.Id}|{entity.Name}";

        private static string[] ToKeys(IEnumerable<EntityBaseSummary> entities) => entities.Select(ToKey).ToArray();

        private static (Dictionary<string, object> Cosmos, BsonDocument Mongo) CreateDocument(string id, string name, bool isProductionReady, bool includeMasterStatus, string orgId = OrganizationId)
        {
            var cosmos = new Dictionary<string, object>
            {
                ["id"] = id,
                ["EntityType"] = EntityType,
                ["Name"] = name,
                ["OwnerOrganization"] = new Dictionary<string, object> { ["Id"] = orgId, ["Text"] = "Parity Organization" },
                ["IsDraft"] = false,
                ["IsDeprecated"] = false,
                ["Revision"] = 1
            };

            var mongo = new BsonDocument
            {
                { "_id", id },
                { "EntityType", EntityType },
                { "Name", name },
                { "OwnerOrganization", new BsonDocument { { "Id", orgId }, { "Text", "Parity Organization" } } },
                { "IsDraft", false },
                { "IsDeprecated", false },
                { "Revision", 1 }
            };

            if (includeMasterStatus)
            {
                cosmos["MasterStatus"] = new Dictionary<string, object> { ["IsProductionReady"] = isProductionReady };
                mongo.Add("MasterStatus", new BsonDocument("IsProductionReady", isProductionReady));
            }

            return (cosmos, mongo);
        }

        private sealed class TestSyncConnectionSettings : ISyncConnectionSettings
        {
            public TestSyncConnectionSettings(IConnectionSettings settings)
            {
                SyncConnectionSettings = settings;
            }

            public IConnectionSettings SyncConnectionSettings { get; }
        }

        [EntityDescription("ParityDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(EntityPreparationCandidateStorageParityTests))]
        private sealed class ParityEntity { }
    }
}
