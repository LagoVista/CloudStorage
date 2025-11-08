// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 22dea970f2aabb53ef73680c0feda9bc68e146cbbdfc0059f47f92b47449ff01
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Tests;
using LagoVista.CloudStorage.Tests.Support;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.IntegrationTests
{
    public class StorageUtilsTest
    {
        private string _accountId;
        private string _accountKey;
        private string _uri;

        const string ORGID = "DDF92E1566C54AA3A8011EE0879D49E3";
        IStorageUtils _storageUtils;

        EntityHeader _orgEH;

        [SetUp]
        public void Setup()
        {
            _orgEH = new EntityHeader()
            {
                Id = ORGID,
                Text = "DELETE ME ORG"
            };

            _accountId = Environment.GetEnvironmentVariable("TEST_DOCDB_ACCOUNTID");
            _accountKey = Environment.GetEnvironmentVariable("TEST_DOCDB_ACCOUTKEY");

            if (String.IsNullOrEmpty(_accountId)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCOUNTID as an environnment variable");
            if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException("Please add TEST_AZURESTORAGE_ACCESSKEY as an environnment variable");

            _uri = $"https://{_accountId}.documents.azure.com:443";
            var dbName = "dev";

            _storageUtils = new StorageUtils(new AdminLogger(new ConsoleLogWriter()), new Mock<ICacheProvider>().Object);
            _storageUtils.SetConnection(new ConnectionSettings()
            {
                ResourceName = dbName,
                Uri = _uri,
                AccessKey = _accountKey,
            }); 
        }

        [Test]
        public async Task GetDefaultInputTranslator()
        {
            await _storageUtils.DeleteByKeyIfExistsAsync<DocDBEntitty>("default", _orgEH);
            var result = await _storageUtils.FindWithKeyAsync<DocDBEntitty>("default");
            Assert.That(result == null);

            await _storageUtils.UpsertDocumentAsync<DocDBEntitty>(new DocDBEntitty()
            {
                OwnerOrganization = _orgEH,
                Key = "default"
            });

            result = await _storageUtils.FindWithKeyAsync<DocDBEntitty>("default", _orgEH);
            Assert.That(result != null);
        }
    }
}