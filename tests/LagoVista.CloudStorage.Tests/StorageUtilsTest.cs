// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 22dea970f2aabb53ef73680c0feda9bc68e146cbbdfc0059f47f92b47449ff01
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Tests;
using LagoVista.CloudStorage.Tests.Support;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.IntegrationTests
{
    public class StorageUtilsTest
    {
        const string DEVORGID = "C8AD4589F26842E7A1AEFBAEFC979C9B";

        IAdminLogger _logger;
        IStorageUtils _storageUtils;
        INodeLocatorTableReader _nodeLocator;
        EntityHeader _orgEH;
        EntityHeader _userEH;

        [SetUp]
        public void Setup()
        {
            _logger = new AdminLogger(new ConsoleLogWriter());
            _orgEH = new EntityHeader(){Id = DEVORGID, Text = "Development Org Id"};
            _userEH = new EntityHeader(){Id = "DDF92E1566C54AA3A8011EE0879D49E3", Text = "DELETE ME USER" };

            var syncSettings = new SyncSettings();
            
            _nodeLocator = new NodeLocatorTableReader(syncSettings, _logger);
            _storageUtils = new StorageUtils(_logger, _nodeLocator, new Mock<ICacheProvider>().Object);
            _storageUtils.SetConnection(syncSettings.DefaultDocDbSettings);
        }

        [Test]
        public async Task BuildObjectGraphTest()
        {
            var graph = await _storageUtils.GetEntityGraphAsync("04AE615F8B4B4E0E9234E4AF200AE3AE", _orgEH, _userEH);
            Console.WriteLine(JsonConvert.SerializeObject(graph, Formatting.Indented));
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