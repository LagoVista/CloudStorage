using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cosmos
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Cosmos")]
    [TestCategory("CosmosDocumentStorageDepth")]
    public sealed class CosmosDocumentStorageClientDepthIntegrationTests
    {
        private const string Endpoint = "https://localhost:18081/";
        private const string AccessKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private CosmosClientProvider _provider;
        private CosmosDocumentStorageClient _client;
        private TestCosmosConnectionSettings _settings;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _provider = new CosmosClientProvider();
            _settings = new TestCosmosConnectionSettings($"cosmos_depth_{Guid.NewGuid():N}");
            var cosmosClient = _provider.GetClient(_settings.Endpoint, _settings.AccessKey);
            var provisioner = new CosmosDocumentCollectionProvisioner();
            await provisioner.EnsureExistsAsync(cosmosClient, _settings.Endpoint, _settings.DatabaseName, $"{_settings.DatabaseName}_Collections", "/EntityType");
            _client = new CosmosDocumentStorageClient(_settings, _provider);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_provider == null || _settings == null) return;

            try
            {
                var client = _provider.GetClient(_settings.Endpoint, _settings.AccessKey);
                await client.GetDatabase(_settings.DatabaseName).DeleteAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task RuntimePatch_ExercisesSetAddRemoveConcurrencyAndMissingPaths()
        {
            var entityType = nameof(CosmosDepthDocumentEntity);
            var id = NewId();
            var json = new JObject
            {
                ["id"] = id,
                ["EntityType"] = entityType,
                ["Key"] = $"depth-{id.ToLowerInvariant()}",
                ["Name"] = "Runtime Patch",
                ["OwnerOrganization"] = new JObject { ["Id"] = "ORG1", ["Text"] = "Organization One" },
                ["Detail"] = "Original",
                ["RemoveMe"] = "present"
            }.ToString();

            var write = await _client.UpsertRawDocumentAsync(entityType, id, json);
            var request = new PatchRequest
            {
                Id = id,
                EntityType = entityType,
                ETag = write.ETag,
                Steps = new[]
                {
                    new PatchStep { Op = PatchOp.Set, LogicalPath = "Detail", Value = JToken.FromObject("Patched") },
                    new PatchStep { Op = PatchOp.Add, LogicalPath = "AddedField", Value = JToken.FromObject("added") },
                    new PatchStep { Op = PatchOp.Remove, LogicalPath = "RemoveMe" }
                }
            };

            var result = await _client.PatchDocumentAsync(entityType, request);
            Assert.IsTrue(result.Successful);

            var patched = await _client.GetDocumentProjectionAsync<JObject>(entityType, id);
            Assert.AreEqual("Patched", patched.Value<string>("Detail"));
            Assert.AreEqual("added", patched.Value<string>("AddedField"));
            Assert.IsNull(patched["RemoveMe"]);
            Assert.AreNotEqual(write.ETag, patched.Value<string>("ETag"));

            await Assert.ThrowsExactlyAsync<ContentModifiedException>(() => _client.PatchDocumentAsync(entityType, new PatchRequest
            {
                Id = id,
                EntityType = entityType,
                ETag = write.ETag,
                Steps = new[] { new PatchStep { Op = PatchOp.Set, LogicalPath = "Detail", Value = JToken.FromObject("stale") } }
            }));

            await Assert.ThrowsExactlyAsync<RecordNotFoundException>(() => _client.PatchDocumentAsync(entityType, new PatchRequest
            {
                Id = NewId(),
                EntityType = entityType,
                Steps = new[] { new PatchStep { Op = PatchOp.Set, LogicalPath = "Detail", Value = JToken.FromObject("missing") } }
            }));
        }

        [TestMethod]
        public async Task PagedQuery_ExercisesVisibilityFiltersAndPagingOverload()
        {
            var active = CreateEntity("keep", "Active", 1);
            var deleted = CreateEntity("keep", "Deleted", 2);
            deleted.IsDeleted = true;
            var draft = CreateEntity("keep", "Draft", 3);
            draft.IsDraft = true;

            await _client.CreateDocumentAsync(active);
            await _client.CreateDocumentAsync(deleted);
            await _client.CreateDocumentAsync(draft);

            var visible = await _client.QueryAsync<CosmosDepthDocumentEntity>(item => item.Detail == "keep", new ListRequest { PageIndex = 1, PageSize = 10 });
            Assert.AreEqual(1, visible.Model.Count());
            Assert.AreEqual(active.Id, visible.Model.Single().Id);

            var all = await _client.QueryAsync<CosmosDepthDocumentEntity>(item => item.Detail == "keep", new ListRequest { PageIndex = 1, PageSize = 10, ShowDeleted = true, ShowDrafts = true });
            Assert.AreEqual(3, all.Model.Count());
        }

        [TestMethod]
        public async Task GenericSort_ExercisesNonStringAscendingAndDescendingSorts()
        {
            await _client.CreateDocumentAsync(CreateEntity("sort", "Thirty", 30));
            await _client.CreateDocumentAsync(CreateEntity("sort", "Ten", 10));
            await _client.CreateDocumentAsync(CreateEntity("sort", "Twenty", 20));

            var ascending = await _client.QueryAsync<CosmosDepthDocumentEntity, int>(item => item.Detail == "sort", item => item.SortOrder, new ListRequest { PageIndex = 1, PageSize = 10 }, false);
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, ascending.Model.Select(item => item.SortOrder).ToArray());

            var descending = await _client.QueryAsync<CosmosDepthDocumentEntity, int>(item => item.Detail == "sort", item => item.SortOrder, new ListRequest { PageIndex = 1, PageSize = 10 }, true);
            CollectionAssert.AreEqual(new[] { 30, 20, 10 }, descending.Model.Select(item => item.SortOrder).ToArray());
        }

        [TestMethod]
        public async Task KnownEntityUtilityQueries_AllCosmosBranchesExecuteWithValidSyntax()
        {
            var entityType = nameof(CosmosDepthDocumentEntity);

            await AssertEmptyAsync(DocumentQueryType.EntityUtilsReadyChecklistCandidates, ChecklistRequest(DocumentQueryType.EntityUtilsReadyChecklistCandidates, entityType).WithParameter("maxItems", 10));
            await AssertCountAsync(DocumentQueryType.EntityUtilsReadyChecklistCount, ChecklistRequest(DocumentQueryType.EntityUtilsReadyChecklistCount, entityType));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsBlockedChecklistCandidates, ChecklistRequest(DocumentQueryType.EntityUtilsBlockedChecklistCandidates, entityType).WithParameter("maxItems", 10));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsCompletedChecklistCandidates, CompletedChecklistRequest(DocumentQueryType.EntityUtilsCompletedChecklistCandidates, entityType).WithParameter("maxItems", 10));
            await AssertCountAsync(DocumentQueryType.EntityUtilsCompletedChecklistCount, CompletedChecklistRequest(DocumentQueryType.EntityUtilsCompletedChecklistCount, entityType));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsDocumentsByFieldValue, BaseRequest(DocumentQueryType.EntityUtilsDocumentsByFieldValue, entityType).WithParameter("fieldName", "Detail").WithParameter("value", "never-match"));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsDocumentsByStatusIds, BaseRequest(DocumentQueryType.EntityUtilsDocumentsByStatusIds, entityType).WithParameter("statusIds", new List<string> { "ACTIVE" }).WithParameter("maxItems", 10));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsDocumentsWithEmptyField, BaseRequest(DocumentQueryType.EntityUtilsDocumentsWithEmptyField, entityType).WithParameter("fieldName", "Description").WithParameter("maxItems", 10));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsDocumentsByType, BaseRequest(DocumentQueryType.EntityUtilsDocumentsByType, entityType));
            await AssertEmptyAsync(DocumentQueryType.EntityUtilsDocumentById, BaseRequest(DocumentQueryType.EntityUtilsDocumentById, entityType).WithParameter("entityId", NewId()));
            await AssertCountAsync(DocumentQueryType.EntityUtilsCountByType, BaseRequest(DocumentQueryType.EntityUtilsCountByType, entityType));
            await AssertEmptyAsync(DocumentQueryType.CustomerIndustryNicheSalesStageCounts, new DocumentQueryRequest(DocumentQueryType.CustomerIndustryNicheSalesStageCounts).WithParameter("orgId", "ORG1"));
        }

        [TestMethod]
        public async Task KnownPreparationAndEntityListQueries_AllCosmosPipelinesExecuteWithValidSyntax()
        {
            var entityType = nameof(CosmosDepthDocumentEntity);

            await AssertEmptyAsync(DocumentQueryType.EntityPreparationCandidateById, BaseRequest(DocumentQueryType.EntityPreparationCandidateById, entityType).WithParameter("entityId", NewId()));
            await AssertEmptyAsync(DocumentQueryType.EntityPreparationCandidatesByType, BaseRequest(DocumentQueryType.EntityPreparationCandidatesByType, entityType));
            await AssertEmptyAsync(DocumentQueryType.IncompleteEntityPreparationCandidatesByType, BaseRequest(DocumentQueryType.IncompleteEntityPreparationCandidatesByType, entityType).WithParameter("maxItems", 10));
            await AssertEmptyAsync(DocumentQueryType.EntityListItems, EntityListRequest(DocumentQueryType.EntityListItems, entityType));
            await AssertEmptyAsync(DocumentQueryType.EntityListHeaders, EntityListRequest(DocumentQueryType.EntityListHeaders, entityType));
            await AssertEmptyAsync(DocumentQueryType.EntityListCategories, EntityListRequest(DocumentQueryType.EntityListCategories, entityType));
        }

        private async Task AssertEmptyAsync(DocumentQueryType queryType, DocumentQueryRequest request)
        {
            var results = (await _client.QueryKnownAsync<JObject>(nameof(CosmosDepthDocumentEntity), request)).ToList();
            Assert.AreEqual(0, results.Count, $"{queryType} should execute successfully against an empty collection.");
        }

        private async Task AssertCountAsync(DocumentQueryType queryType, DocumentQueryRequest request)
        {
            var results = (await _client.QueryKnownAsync<DocumentCountResult>(nameof(CosmosDepthDocumentEntity), request)).ToList();
            Assert.AreEqual(1, results.Count, $"{queryType} should return its count envelope.");
            Assert.AreEqual(0, results[0].Count);
        }

        private static DocumentQueryRequest BaseRequest(DocumentQueryType queryType, string entityType) => new DocumentQueryRequest(queryType).WithParameter("entityType", entityType).WithParameter("orgId", "ORG1");

        private static DocumentQueryRequest ChecklistRequest(DocumentQueryType queryType, string entityType) => BaseRequest(queryType, entityType).WithParameter("requiredStepKeys", new List<string> { "prerequisite" }).WithParameter("targetStepKeys", new List<string> { "target" });

        private static DocumentQueryRequest CompletedChecklistRequest(DocumentQueryType queryType, string entityType) => BaseRequest(queryType, entityType).WithParameter("stepKeys", new List<string> { "complete" });

        private static DocumentQueryRequest EntityListRequest(DocumentQueryType queryType, string entityType)
        {
            return BaseRequest(queryType, entityType)
                .WithParameter("showDeleted", false)
                .WithParameter("showDrafts", false)
                .WithParameter("categoryKey", String.Empty)
                .WithParameter("statusKey", String.Empty)
                .WithParameter("labelKey", String.Empty)
                .WithParameter("searchText", String.Empty)
                .WithParameter("orderBy", 0)
                .WithParameter("descending", false)
                .WithParameter("pageIndex", 1)
                .WithParameter("pageSize", 10);
        }

        private static CosmosDepthDocumentEntity CreateEntity(string detail, string name, int sortOrder)
        {
            var id = NewId();
            return new CosmosDepthDocumentEntity
            {
                Id = id,
                Key = $"depth-{id.ToLowerInvariant()}",
                Name = name,
                EntityType = nameof(CosmosDepthDocumentEntity),
                OwnerOrganization = EntityHeader.Create("ORG1", "Organization One"),
                Detail = detail,
                SortOrder = sortOrder
            };
        }

        private static string NewId() => Guid.NewGuid().ToString("N").ToUpperInvariant();

        private sealed class TestCosmosConnectionSettings : ICosmosConnectionSettings
        {
            public TestCosmosConnectionSettings(string databaseName)
            {
                DatabaseName = databaseName;
            }

            public string Endpoint => CosmosDocumentStorageClientDepthIntegrationTests.Endpoint;
            public string AccessKey => CosmosDocumentStorageClientDepthIntegrationTests.AccessKey;
            public string DatabaseName { get; }
        }
    }

    internal sealed class CosmosDepthDocumentEntity : EntityBase
    {
        public string Detail { get; set; }
        public int SortOrder { get; set; }
    }
}
