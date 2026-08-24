using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
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
    public class MongoDocumentCollectionIntegrationTests
    {
        private const string CollectionName = "DocumentCollectionTests";
        private string _connectionString;
        private string _databaseName;
        private IMongoClient _client;
        private MongoDocumentCollection _documentCollection;

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _connectionString = GetTestConnectionString();
            _databaseName = $"CloudStorageTests_{Guid.NewGuid():N}";
            _client = new MongoClient(_connectionString);
            _documentCollection = new MongoDocumentCollection(_connectionString, _databaseName, CollectionName);
            var collection = _client.GetDatabase(_databaseName).GetCollection<TestDocument>(CollectionName);
            await collection.InsertManyAsync(new[]
            {
                CreateDocument("DOC3", "Gamma", 3, "TestEntity", "ORG1"),
                CreateDocument("DOC1", "Alpha", 1, "TestEntity", "ORG1"),
                CreateDocument("DOC2", "Beta", 2, "TestEntity", "ORG1"),
                CreateDocument("OTHER", "Other", 4, "OtherEntity", "ORG1"),
                CreateCustomer("CUSTOMER1", "Customer One", "ORG1", "IND1", "NICHE1", "QUALIFIED"),
                CreateCustomer("CUSTOMER2", "Customer Two", "ORG1", "IND1", "NICHE1", "QUALIFIED"),
                CreateCustomer("CUSTOMER3", "Customer Three", "ORG1", "IND1", "NICHE2", "NEW"),
                CreateCustomer("CUSTOMER4", "Customer Other Org", "ORG2", "IND1", "NICHE1", "QUALIFIED")
            });
        }

        [OneTimeTearDown]
        public async Task TearDownAsync()
        {
            if (_client != null && !String.IsNullOrWhiteSpace(_databaseName)) await _client.DropDatabaseAsync(_databaseName);
        }

        [Test]
        public async Task QueryAsync_WithFilterSortAndPaging_ReturnsExpectedPage()
        {
            var request = new ListRequest { PageIndex = 1, PageSize = 2 };
            var result = await _documentCollection.QueryAsync<TestDocument>(document => document.EntityType == "TestEntity", document => document.Name, request);
            Assert.That(result.Model.Select(document => document.Name), Is.EqualTo(new[] { "Alpha", "Beta" }));
        }

        [Test]
        public async Task QueryAsync_WithPagedProjection_ReturnsProjectedDocuments()
        {
            var request = new ListRequest { PageIndex = 1, PageSize = 2 };
            var result = await _documentCollection.QueryAsync<TestDocument, TestProjection, int>(document => document.EntityType == "TestEntity", document => new TestProjection { Id = document.Id, Name = document.Name }, document => document.Rank, request);
            Assert.That(result.Model.Select(document => document.Name), Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(result.Model.All(document => !String.IsNullOrWhiteSpace(document.Id)), Is.True);
        }

        [Test]
        public async Task QueryAsync_WithUnpagedProjection_ReturnsAllMatchingDocuments()
        {
            var result = await _documentCollection.QueryAsync<TestDocument, TestProjection>(document => document.EntityType == "TestEntity", document => new TestProjection { Id = document.Id, Name = document.Name });
            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task QueryKnownAsync_WithCustomerMetricsAggregate_ReturnsServerSideCounts()
        {
            var request = new KnownDocumentQueryRequest(KnownDocumentQuery.CustomerIndustryNicheSalesStageCounts).WithParameter("orgId", "ORG1");
            var results = (await _documentCollection.QueryKnownAsync<TestCustomerMetrics>(request)).ToList();
            Assert.That(results.Sum(result => result.CountLeads), Is.EqualTo(3));
            Assert.That(results.Single(result => result.Industry.Id == "IND1" && result.IndustryNiche.Id == "NICHE1" && result.SalesStage.Id == "QUALIFIED").CountLeads, Is.EqualTo(2));
        }

        [Test]
        public async Task RawDocumentAdapter_FilterGetAndCount_ReturnExpectedDocuments()
        {
            var filter = new DocumentFilterRequest()
                .WhereEquals("EntityType", "TestEntity")
                .WhereEquals("OwnerOrganization.Id", "ORG1")
                .OrderBy("Name");

            var documents = (await _documentCollection.QueryDocumentsAsync(filter)).ToList();
            Assert.That(documents.Select(document => (string)document["Name"]), Is.EqualTo(new[] { "Alpha", "Beta", "Gamma" }));
            Assert.That(await _documentCollection.CountDocumentsAsync(filter), Is.EqualTo(3));

            var document = await _documentCollection.GetDocumentAsync("DOC2");
            Assert.That((string)document?["id"], Is.EqualTo("DOC2"));
            Assert.That((string)document?["Name"], Is.EqualTo("Beta"));
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

        private static TestDocument CreateDocument(string id, string name, int rank, string entityType, string orgId)
        {
            return new TestDocument
            {
                Id = id,
                Name = name,
                Rank = rank,
                EntityType = entityType,
                OwnerOrganization = EntityHeader.Create(orgId, orgId)
            };
        }

        private static TestDocument CreateCustomer(string id, string name, string orgId, string industryId, string nicheId, string salesStageId)
        {
            return new TestDocument
            {
                Id = id,
                Name = name,
                EntityType = "CustomerEntity",
                OwnerOrganization = EntityHeader.Create(orgId, orgId),
                Industry = EntityHeader.Create(industryId, industryId),
                IndustryNiche = EntityHeader.Create(nicheId, nicheId),
                SalesStage = EntityHeader.Create(salesStageId, salesStageId)
            };
        }

        private sealed class TestDocument
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public string Name { get; set; }
            public int Rank { get; set; }
            public EntityHeader OwnerOrganization { get; set; }
            public EntityHeader Industry { get; set; }
            public EntityHeader IndustryNiche { get; set; }
            public EntityHeader SalesStage { get; set; }
        }

        private sealed class TestProjection
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private sealed class TestCustomerMetrics
        {
            public EntityHeader Industry { get; set; }
            public EntityHeader IndustryNiche { get; set; }
            public EntityHeader SalesStage { get; set; }
            public int CountLeads { get; set; }
        }
    }
}
