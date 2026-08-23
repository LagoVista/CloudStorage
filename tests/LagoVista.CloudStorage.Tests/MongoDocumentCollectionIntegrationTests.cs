using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    public class MongoDocumentCollectionIntegrationTests
    {
        private const string ConnectionStringEnvironmentVariable = "NUVIOT_TEST_MONGO_CONNECTION_STRING";
        private const string CollectionName = "DocumentCollectionTests";
        private string _connectionString;
        private string _databaseName;
        private IMongoClient _client;
        private MongoDocumentCollection _documentCollection;

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            if (String.IsNullOrWhiteSpace(_connectionString)) Assert.Ignore($"Set {ConnectionStringEnvironmentVariable} to run Mongo integration tests.");

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
        public async Task QueryAsync_WithCustomerMetricsAggregate_ReturnsServerSideCounts()
        {
            var request = new DocumentQueryRequest(DocumentQueryType.CustomerIndustryNicheSalesStageCounts).WithParameter("orgId", "ORG1");
            var results = (await _documentCollection.QueryAsync<TestCustomerMetrics>(request)).ToList();
            Assert.That(results.Sum(result => result.CountLeads), Is.EqualTo(3));
            Assert.That(results.Single(result => result.Industry.Id == "IND1" && result.IndustryNiche.Id == "NICHE1" && result.SalesStage.Id == "QUALIFIED").CountLeads, Is.EqualTo(2));
        }

        private static TestDocument CreateDocument(string id, string name, int rank, string entityType, string orgId)
        {
            return new TestDocument
            {
                Id = id,
                Name = name,
                Rank = rank,
                EntityType = entityType,
                OwnerOrganization = new TestHeader { Id = orgId, Text = orgId }
            };
        }

        private static TestDocument CreateCustomer(string id, string name, string orgId, string industryId, string nicheId, string salesStageId)
        {
            return new TestDocument
            {
                Id = id,
                Name = name,
                EntityType = "CustomerEntity",
                OwnerOrganization = new TestHeader { Id = orgId, Text = orgId },
                Industry = new TestHeader { Id = industryId, Text = industryId },
                IndustryNiche = new TestHeader { Id = nicheId, Text = nicheId },
                SalesStage = new TestHeader { Id = salesStageId, Text = salesStageId }
            };
        }

        private sealed class TestDocument
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public string Name { get; set; }
            public int Rank { get; set; }
            public TestHeader OwnerOrganization { get; set; }
            public TestHeader Industry { get; set; }
            public TestHeader IndustryNiche { get; set; }
            public TestHeader SalesStage { get; set; }
        }

        private sealed class TestProjection
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private sealed class TestCustomerMetrics
        {
            public TestHeader Industry { get; set; }
            public TestHeader IndustryNiche { get; set; }
            public TestHeader SalesStage { get; set; }
            public int CountLeads { get; set; }
        }

        private sealed class TestHeader
        {
            public string Id { get; set; }
            public string Text { get; set; }
        }
    }
}
