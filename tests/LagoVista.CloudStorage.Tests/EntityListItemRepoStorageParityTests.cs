using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.AI.Interfaces;
using LagoVista.Core.Attributes;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
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
    public class EntityListItemRepoStorageParityTests
    {
        private const string OrganizationId = "ORG-PARITY";
        private const string EntityType = nameof(ParityListEntity);

        [Test]
        public Task GetEntityHeadersAsync_CosmosAndMongo_ApplySameVisibilityAndLifecycleFilters() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var request = new ListRequest { PageIndex = 1, PageSize = 20 };
                var cosmosResult = await cosmos.GetEntityHeadersAsync(OrganizationId, request);
                var mongoResult = await mongo.GetEntityHeadersAsync(OrganizationId, request);

                Assert.That(ToHeaderKeys(mongoResult.Model), Is.EqualTo(ToHeaderKeys(cosmosResult.Model)));
                Assert.That(cosmosResult.Model.Select(item => item.Text).ToArray(), Is.EqualTo(new[] { "Alpha", "Beta", "Foxtrot" }));
            });

        [Test]
        public Task GetListItemsAsync_CosmosAndMongo_ReturnSameProjectedItems() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var request = new ListRequest { PageIndex = 1, PageSize = 20 };
                var cosmosResult = await cosmos.GetListItemsAsync(OrganizationId, request);
                var mongoResult = await mongo.GetListItemsAsync(OrganizationId, request);

                Assert.That(mongoResult.Model.Select(item => $"{item.Name}|{item.Category}|{item.Stars}").ToArray(),
                    Is.EqualTo(cosmosResult.Model.Select(item => $"{item.Name}|{item.Category}|{item.Stars}").ToArray()));
                Assert.That(cosmosResult.Model.Select(item => item.Name).ToArray(), Is.EqualTo(new[] { "Alpha", "Beta", "Foxtrot" }));
            });

        [Test]
        public Task GetEntityHeadersAsync_CosmosAndMongo_ApplySameCategoryStatusLabelAndSearchFilters() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var request = new ListRequest
                {
                    PageIndex = 1,
                    PageSize = 20,
                    CategoryKey = "cat-a",
                    StatusKey = "paused",
                    LabelKey = "label-two",
                    SearchText = "FOX"
                };

                var cosmosResult = await cosmos.GetEntityHeadersAsync(OrganizationId, request);
                var mongoResult = await mongo.GetEntityHeadersAsync(OrganizationId, request);

                Assert.That(ToHeaderKeys(mongoResult.Model), Is.EqualTo(ToHeaderKeys(cosmosResult.Model)));
                Assert.That(cosmosResult.Model.Select(item => item.Text).ToArray(), Is.EqualTo(new[] { "Foxtrot" }));
            });

        [Test]
        public Task GetEntityHeadersAsync_CosmosAndMongo_ApplySamePaging() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var request = new ListRequest { PageIndex = 2, PageSize = 1 };
                var cosmosResult = await cosmos.GetEntityHeadersAsync(OrganizationId, request);
                var mongoResult = await mongo.GetEntityHeadersAsync(OrganizationId, request);

                Assert.That(ToHeaderKeys(mongoResult.Model), Is.EqualTo(ToHeaderKeys(cosmosResult.Model)));
                Assert.That(cosmosResult.Model.Single().Text, Is.EqualTo("Beta"));
            });

        [Test]
        public Task GetListItemsAsync_CosmosAndMongo_ApplySameRatingSortAndCategoryOptions() =>
            WithRepositoriesAsync(async (cosmos, mongo) =>
            {
                var request = new ListRequest { PageIndex = 1, PageSize = 2, OrderByDesc = OrderByTypes.Rating };
                var cosmosResult = await cosmos.GetListItemsAsync(OrganizationId, request);
                var mongoResult = await mongo.GetListItemsAsync(OrganizationId, request);

                Assert.That(mongoResult.Model.Select(item => item.Name).ToArray(), Is.EqualTo(cosmosResult.Model.Select(item => item.Name).ToArray()));
                Assert.That(cosmosResult.Model.Select(item => item.Name).ToArray(), Is.EqualTo(new[] { "Beta", "Foxtrot" }));
                Assert.That(mongoResult.Categories.Select(item => item.Key).ToArray(), Is.EqualTo(cosmosResult.Categories.Select(item => item.Key).ToArray()));
                Assert.That(cosmosResult.Categories.Skip(1).Select(item => item.Key).ToArray(), Is.EqualTo(new[] { "cat-a", "cat-b" }));
            });

        private static async Task WithRepositoriesAsync(Func<EntityListItemRepo<ParityListEntity>, EntityListItemRepo<ParityListEntity>, Task> assertion)
        {
            var cosmosDatabaseName = $"ListParityCosmos_{Guid.NewGuid():N}";
            var mongoLogicalDatabaseName = $"ListParityMongo{Guid.NewGuid():N}";
            var mongoDatabaseName = $"ListParityMongo_{Guid.NewGuid():N}";
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
                var mongoCollection = mongoClient.GetDatabase(mongoDatabaseName).GetCollection<BsonDocument>("ListParityDomain");

                var documents = new[]
                {
                    CreateDocument("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Alpha", OrganizationId, false, false, false, "cat-a", "Category A", "active", "label-one", 3),
                    CreateDocument("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", "Beta", "ORG-OTHER", true, false, false, "cat-b", "Category B", "active", "label-one", 5),
                    CreateDocument("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", "Charlie", OrganizationId, false, true, false, "cat-c", "Category C", "active", "label-one", 2),
                    CreateDocument("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD", "Delta", OrganizationId, false, false, true, "cat-d", "Category D", "active", "label-one", 1),
                    CreateDocument("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE", "Echo", "ORG-OTHER", false, false, false, "cat-e", "Category E", "active", "label-one", 4),
                    CreateDocument("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "Foxtrot", OrganizationId, false, false, false, "cat-a", "Category A", "paused", "label-two", 4)
                };

                foreach (var document in documents)
                {
                    await cosmosContainer.CreateItemAsync(document.Cosmos, new PartitionKey(EntityType));
                    await mongoCollection.InsertOneAsync(document.Mongo);
                }

                var logger = new AdminLogger(new ConsoleLogWriter());
                var services = new TestCloudServices(logger, cosmosProvider);
                var cosmosRepository = new EntityListItemRepo<ParityListEntity>(StorageLabConnections.CosmosEndpoint, StorageLabConnections.CosmosKey, cosmosDatabaseName, services);
                var mongoRepository = new EntityListItemRepo<ParityListEntity>("https://cosmos-unused.example/", null, mongoLogicalDatabaseName, services);

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

        private static string[] ToHeaderKeys(IEnumerable<EntityHeader> headers) => headers.Select(item => $"{item.Id}|{item.Text}").ToArray();

        private static (Dictionary<string, object> Cosmos, BsonDocument Mongo) CreateDocument(string id, string name, string orgId, bool isPublic, bool isDraft, bool isDeleted, string categoryKey, string categoryText, string statusKey, string labelKey, double stars)
        {
            var categoryId = categoryKey.ToUpperInvariant();
            var cosmos = new Dictionary<string, object>
            {
                ["id"] = id,
                ["EntityType"] = EntityType,
                ["Name"] = name,
                ["OwnerOrganization"] = new Dictionary<string, object> { ["Id"] = orgId, ["Text"] = orgId },
                ["IsPublic"] = isPublic,
                ["IsDraft"] = isDraft,
                ["IsDeleted"] = isDeleted,
                ["Category"] = new Dictionary<string, object> { ["Id"] = categoryId, ["Key"] = categoryKey, ["Text"] = categoryText },
                ["Status"] = new Dictionary<string, object> { ["Id"] = statusKey.ToUpperInvariant(), ["Key"] = statusKey, ["Text"] = statusKey },
                ["Labels"] = new[] { new Dictionary<string, object> { ["Id"] = labelKey.ToUpperInvariant(), ["Key"] = labelKey, ["Text"] = labelKey } },
                ["Stars"] = stars,
                ["RatingsCount"] = 1
            };

            var mongo = new BsonDocument
            {
                { "_id", id }, { "EntityType", EntityType }, { "Name", name },
                { "OwnerOrganization", new BsonDocument { { "Id", orgId }, { "Text", orgId } } },
                { "IsPublic", isPublic }, { "IsDraft", isDraft }, { "IsDeleted", isDeleted },
                { "Category", new BsonDocument { { "Id", categoryId }, { "Key", categoryKey }, { "Text", categoryText } } },
                { "Status", new BsonDocument { { "Id", statusKey.ToUpperInvariant() }, { "Key", statusKey }, { "Text", statusKey } } },
                { "Labels", new BsonArray { new BsonDocument { { "Id", labelKey.ToUpperInvariant() }, { "Key", labelKey }, { "Text", labelKey } } } },
                { "Stars", stars }, { "RatingsCount", 1 }
            };

            return (cosmos, mongo);
        }

        private sealed class TestCloudServices : IDocumentCloudCachedServices
        {
            public TestCloudServices(IAdminLogger logger, ICosmosClientProvider cosmosClientProvider)
            {
                AdminLogger = logger;
                CosmosClientProvider = cosmosClientProvider;
            }

            public IAdminLogger AdminLogger { get; }
            public ICosmosClientProvider CosmosClientProvider { get; }
            public ICacheProvider CacheProvider => null;
            public ICacheAborter CacheAborter => null;
            public IEntityListCacheInvalidator EntityListCacheInvalidator => null;
            public IDependencyManager DependencyManager => null;
            public IProducedArtifactService ProducedArtifactService => null;
            public IUserNotificationService UserNotificationService => null;
            public IRagIndexingServices RagIndexingServices => null;
            public IFkIndexTableWriterBatched FkIndexTableWriter => null;
        }

        [EntityDescription("ListParityDomain", "", "", "", EntityDescriptionAttribute.EntityTypes.Dto, typeof(EntityListItemRepoStorageParityTests))]
        private sealed class ParityListEntity : EntityBase { }
    }
}
