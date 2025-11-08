// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: a61d6384fa8912252e7e1ced044ceab80c1c4bb606df9221f23a89f7fa72c475
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentStorageTests
    {
        private string _accountId;
        private string _accountKey;
        private string _uri;

        const string DBNAME = "UnitTest";

        const string ORGID = "DDF92E1566C54AA3A8011EE0879D49E3";

        Support.DocDBEntityRepo _docDBEntityRepo;

        [SetUp]
        public void Setup()
        {
            _accountId = Environment.GetEnvironmentVariable("TEST_DOCDB_ACCOUNTID");
            _accountKey = Environment.GetEnvironmentVariable("TEST_DOCDB_ACCOUTKEY");

            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCOUNTID as an environnment variable");
            if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCESSKEY as an environnment variable");

            _uri = $"https://{_accountId}.documents.azure.com:443";
            var dbName = DBNAME;

            _docDBEntityRepo = new Support.DocDBEntityRepo(new Uri(_uri), _accountKey, dbName, new AdminLogger(new ConsoleLogWriter()));
        }

        [Test]
        public async Task CreateDocumentTest()
        {
            await _docDBEntityRepo!.CreateDBDocment(new Support.DocDBEntitty() { Id = Guid.NewGuid().ToId() });
        }
        class TaskSummary
        {
            public string id { get; set; }
            public string Name { get; set; }
            public string StatusId { get; set; }
            public string Status { get; set; }
        }


        [Test]
        public async Task GetDocs()
        {
            var summaries = await _docDBEntityRepo.GetDocumentSummary<TaskSummary>();

            summaries = await _docDBEntityRepo.GetDocumentSummary<TaskSummary>();
            Console.WriteLine("Response size" + summaries.Count);
            foreach (var summary in summaries)
            {
                //     Console.WriteLine(summary.id);
            }
        }


        [Test]
        public async Task CreateAndGetDocumentTest()
        {
            var newDoc = CreateDoc();
            await _docDBEntityRepo!.CreateDBDocment(newDoc);

            var existing = await _docDBEntityRepo.GetDocDBEntitty(newDoc.Id!);
            Assert.That(newDoc.Name == existing.Name);
        }

        [Test]
        public async Task CreateAndUpdateDocumentTest()
        {
            var newDoc = CreateDoc();
            await _docDBEntityRepo!.CreateDBDocment(newDoc);

            var existing = await _docDBEntityRepo.GetDocDBEntitty(newDoc.Id!);
            Assert.That(newDoc.Name == existing.Name);

            existing.Name = Guid.NewGuid().ToString();

            await _docDBEntityRepo.UpdateDBDocument(existing);

            var updatedDoc = await _docDBEntityRepo.GetDocDBEntitty(newDoc.Id!);

            Assert.That(existing.Name == updatedDoc.Name);
        }

        [Test]

        public async Task DeleteDocumentTest()
        {
            var newDoc = CreateDoc();
            await _docDBEntityRepo!.CreateDBDocment(newDoc);

            var existing = await _docDBEntityRepo.GetDocDBEntitty(newDoc.Id!);
            Assert.That(newDoc.Name == existing.Name);

            existing.Name = Guid.NewGuid().ToString();

            await _docDBEntityRepo.DeleteDocDBEntity(newDoc.Id!);

            Assert.ThrowsAsync<RecordNotFoundException>(async () => await _docDBEntityRepo.GetDocDBEntitty(newDoc.Id!));
        }

        [Test]
        public async Task CreatePagedRequest()
        {
            await Create200DocsAsync();

            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 1,
            };

            var results = await _docDBEntityRepo!.GetForOrgAsync(ORGID, rqst);

            Assert.That(50 == results.Model.Count());
            Assert.That(50 == results.PageSize);
        }

        [Test]
        public async Task CreatePagedRequest_Page2()
        {
            await Create200DocsAsync();

            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 2,
            };

            var results = await _docDBEntityRepo!.GetForOrgAsync(ORGID, rqst);

            Assert.That(50 == results.PageSize);
            Assert.That(50 == results.Model.Count());
            Assert.That(2 == results.PageIndex);
            Assert.That(50 == results.Model.First().Index);
        }


        [Test]
        public async Task CreatePagedRequestDesc()
        {
            await Create200DocsAsync();

            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 1,
            };

            var results = await _docDBEntityRepo!.GetForOrgDescAsync(ORGID, rqst);
            Assert.That(99 == results.Model.First().Index);
            Assert.That(50 == results.Model.Count());


            rqst.PageIndex = 2;
            results = await _docDBEntityRepo!.GetForOrgDescAsync(ORGID, rqst);
            Assert.That(49 == results.Model.First().Index);
            Assert.That(50 == results.Model.Count());
        }


        [Test]
        public async Task CreatePagedRequestAll()
        {
            await Create200DocsAsync();

            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 1,
            };

            var results = await _docDBEntityRepo!.GetForOrgAllAsync(ORGID, rqst);

            Assert.That(50 == results.Model.Count());
            Assert.That(rqst.PageSize == results.PageSize);
        }

        private async Task Create200DocsAsync()
        {

            for (var idx = 0; idx < 100; idx++)
            {
                var doc = CreateDoc(idx: idx);
                doc.Index = idx;
                await _docDBEntityRepo!.CreateDBDocment(doc);
            }

            const string ORGID2 = "ACDC2E1566C54AA3A8011EE0879DACDC";

            for (var idx = 200; idx < 300; idx++)
            {
                var doc = CreateDoc(ORGID2, idx: idx);
                doc.Index = idx;
                await _docDBEntityRepo!.CreateDBDocment(doc);
            }
        }

        [Test]
        public async Task CreateAndQueryManyDocumentsTest()
        {
            await Create200DocsAsync();
            var records = await _docDBEntityRepo!.GetForOrgAsync(ORGID);
            Assert.That(100 == records.Count());
        }


        [Test]
        public async Task CreateAndQueryManyDocumentsWithSQLTest()
        {
            await Create200DocsAsync();
            var records = await _docDBEntityRepo!.GetForOrgWithSQL(ORGID);
            Assert.That(100 == records.Count());
        }

        [Test]
        public async Task Create_Document_Performance_Test()
        {
            await _docDBEntityRepo!.CreateDBDocment(new Support.DocDBEntitty() { Id = Guid.NewGuid().ToId() });

            // we create a new instance but we don't need the overhead of checking to see if the DB or collection were created.
            _docDBEntityRepo = new Support.DocDBEntityRepo(new Uri(_uri!), _accountKey!, DBNAME, new AdminLogger(new ConsoleLogWriter()));
            await _docDBEntityRepo!.CreateDBDocment(CreateDoc());
        }

        [Test]
        public async Task Get_Document_Query_OrderBy()
        {
            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 1,
            };

            var result = await _docDBEntityRepo!.GetOrderedQuery(rqst);
            Assert.That(result.Successful, result.Errors.FirstOrDefault()?.Message);
        }


        [Test]
        public async Task Get_Document_Query_Summary_OrderBy()
        {
            await Create200DocsAsync();
            var rqst = new ListRequest()
            {
                PageSize = 50,
                PageIndex = 1,
            };

            var result = await _docDBEntityRepo!.GetOrderedSummaryQuery(rqst);
            Assert.That(result.Successful, result.Errors.FirstOrDefault()?.Message);
        }

        private Support.DocDBEntitty CreateDoc(string orgId = null, int? idx = null)
        {
            return new Support.DocDBEntitty()
            {
                Id = Guid.NewGuid().ToId(),
                Name = idx.HasValue ? $"Index {idx.Value}" : Guid.NewGuid().ToString(),
                Key = Guid.NewGuid().ToId().ToLower().Substring(0, 20),
                OwnerOrganization = EntityHeader.Create(orgId ?? ORGID, "TEST ORDER")
            };
        }

        [TearDown]
        public async Task CleanUp()
        {
            var uri = $"https://{_accountId}.documents.azure.com:443";
            var client = new CosmosClient(uri, _accountKey);

            var db = client.GetDatabase(DBNAME);
            await db.DeleteAsync();
        }
    }
}