using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.IntegrationTests
{
    public class NodeWalkerTests
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
            _syncRepo = new CosmosSyncRepository(new SyncSettings(), _fkeyWriter, _nodeWriter, _nodeReader, new Mock<ICacheProvider>().Object, new AdminLogger(new ConsoleLogWriter()));
        }

        [Test]
        public async Task ExtractNodes()
        {
            string contents = String.Empty;
            string token = null;
            var fn = @"X:\Nodes.txt";
            if(System.IO.File.Exists(fn))
            {
                contents = System.IO.File.ReadAllText(fn);
                var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);   
                token = lines.Last();
            }


            var result = await _syncRepo.AddNodeLocatorsAsync(token, 100, 10);
            if (result.Successful)
            {
                contents += $"{DateTime.UtcNow.ToJSONString()} - Records Processed. {result.Result.Entries.Count}{Environment.NewLine}";
                contents += result.Result.ContinuationToken + Environment.NewLine;
                System.IO.File.WriteAllText(fn, contents);
            }
            else
                throw new Exception(result.ErrorMessage);
        }
    }
}
