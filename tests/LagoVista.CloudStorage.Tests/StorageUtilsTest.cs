using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Tests;
using LagoVista.CloudStorage.Tests.Support;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
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

            _storageUtils = new StorageUtils(new AdminLogger());
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
            Assert.IsNull(result);

            await _storageUtils.UpsertDocumentAsync<DocDBEntitty>(new DocDBEntitty()
            {
                OwnerOrganization = _orgEH,
                Key = "default"
            });

            result = await _storageUtils.FindWithKeyAsync<DocDBEntitty>("default", _orgEH);
            Assert.IsNotNull(result);
        }
    }
}