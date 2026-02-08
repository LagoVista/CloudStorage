using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.IntegrationTests
{
    public class SyncIntegrationTests
    {
        ISyncRepository _syncRepo;
        IFkIndexTableWriterBatched _fkeyWriter;
        INodeLocatorTableWriterBatched _nodeWriter;
        INodeLocatorTableReader _nodeReader;
    IAdminLogger _logger;

        [SetUp]
        public void Setup()
        {
            _logger = new AdminLogger(new ConsoleLogWriter());
            _fkeyWriter = new FkIndexTableWriterBatched(new SyncSettings(), _logger);
            _nodeWriter = new NodeLocatorTableWriterBatched(new SyncSettings(), _logger);
            _nodeReader = new NodeLocatorTableReader(new SyncSettings(), _logger);
            _syncRepo = new CosmosSyncRepository(new SyncSettings(), _fkeyWriter, _nodeWriter,_nodeReader, new Mock<ICacheProvider>().Object, new AdminLogger(new ConsoleLogWriter()));
        }


        [Test]
        public async Task GetOrgEntityHeaderAsync()
        {
            var eh = await _syncRepo.GetEntityHeaderForRecordAsync("C8AD4589F26842E7A1AEFBAEFC979C9B");
            Console.WriteLine($"Id: {eh.Id}, Key: {eh.Key}, EntityType: {eh.EntityType}, Text: {eh.Text}");
        }

        [Test]
        public async Task GetUserEntityHeaderAsync()
        {
            var eh = await _syncRepo.GetEntityHeaderForRecordAsync("0C1AD6B8E40845C390725733635B354E");
            Console.WriteLine($"Id: {eh.Id}, Key: {eh.Key}, EntityType: {eh.EntityType}, Text: {eh.Text}");
        }

        [Test]
        public async Task ResolveFksys()
        {
            var result = await _syncRepo.ResolveEntityHeadersAsync("41CA44CB485D4B3BA751F0DAAC3E1F76");
        }

        [Test]
        public async Task ScanContainer()
        {
            string continuationToken = null;
            var ct = await _syncRepo.ScanContainerAsync(async (row, ct) =>
            {
                await _syncRepo.ResolveEntityHeadersAsync(row.Id, dryRun: false);
                Console.WriteLine(row.Id);
            }, null, continuationToken, 50, 1, null);
        }

        [Test]
        public async Task UpsertEntity()
        {
            var json = await _syncRepo.GetJsonByIdAsync("41CA44CB485D4B3BA751F0DAAC3E1F76");
            var entity = JsonConvert.DeserializeObject<EntityBase>(json);

            await _syncRepo.UpsertJsonAsync(json, entity.OwnerOrganization, entity.CreatedBy);
        }

        [Test]
        public async Task ResolveEntityForToolBox()
        {
            var result = await _syncRepo.ResolveEntityHeadersAsync("AgentToolBox", null);
            Console.WriteLine($"Resolved {result.Result.Count} entities, Updated: {result.Result.Where(res => res.UpdatedEntity).Count()}");
        }


        [Test]
        public async Task ResolveEntityForDetailDesignReview()
        {
            var result = await _syncRepo.ResolveEntityHeadersAsync("DetailedDesignReview", null);
            Console.WriteLine($"Resolved {result.Result.Count} entities, Updated: {result.Result.Where(res => res.UpdatedEntity).Count()}");
        }
    }

    class SyncSettings : ISyncConnectionSettings, IDefaultConnectionSettings
    {
        public IConnectionSettings SyncConnectionSettings => Utils.TestConnections.DevDocDB;

        public IConnectionSettings DefaultDocDbSettings => Utils.TestConnections.DevDocDB;

        public IConnectionSettings DefaultTableStorageSettings => Utils.TestConnections.DevTableStorageDB;

        public IConnectionSettings EHCheckPointStorageSettings => throw new NotImplementedException();
    }
    
}
