// See https://aka.ms/new-console-template for more information
using Azure.Data.Tables;
using DataMigration;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Utils.TableSizer;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Utils;
using Moq;
using System.Diagnostics;

ISyncRepository _syncRepo;
IFkIndexTableWriterBatched _fkeyWriter;
INodeLocatorTableWriterBatched _nodeWriter;
IAdminLogger _logger;

var mode = "deletetablerows";
var env = "prod";
var entityType = "ExternalWorkTask";
var pageSize = 500;
var pageCount = 10;
var dryRun = false;

var syncSettings = new SyncSettings(env);

_logger = new AdminLogger(new ConsoleLogWriter());
_fkeyWriter = new FkIndexTableWriterBatched(syncSettings, _logger);
_nodeWriter = new NodeLocatorTableWriterBatched(syncSettings, _logger);
_syncRepo = new CosmosSyncRepository(syncSettings, _fkeyWriter, _nodeWriter, new Mock<ICacheProvider>().Object, new AdminLogger(new ConsoleLogWriter()));

CancellationTokenSource _shutdownCts = new CancellationTokenSource();


void HookShutdownSignals()
{
    // Ctrl+C
    Console.CancelKeyPress += (sender, e) =>
    {
        Console.WriteLine("Ctrl+C received, shutting down...");
        e.Cancel = true; // prevent immediate process termination
        _shutdownCts.Cancel();
    };

    // Process exit (window close, service stop, kill, etc.)
    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
    {
        if (!_shutdownCts.IsCancellationRequested)
        {
            Console.WriteLine("Process exit detected, shutting down...");
            _shutdownCts.Cancel();
        }
    };
}


async Task BuildNodeLocatorIndexAsync(CancellationToken ct)
{
    var sw = Stopwatch.StartNew();

    string contents = String.Empty;
    string token = null;
    var fn = @"X:\Nodes.txt";
    if (System.IO.File.Exists(fn))
    {
        contents = System.IO.File.ReadAllText(fn);
        var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        token = lines.Last();
    }

    const int pageSize = 100;
    const int pageCount = 10;

    var result = await _syncRepo.AddNodeLocatorsAsync(token, pageSize, pageCount);
    if (result.Successful)
    {
        contents += $"{DateTime.UtcNow.ToJSONString()} - Records Processed. {result.Result.Entries.Count}{Environment.NewLine}";
        if (String.IsNullOrEmpty(result.Result.ContinuationToken))
            contents += $"[COMPLETE] - {DateTime.UtcNow.ToJSONString()} - No more records to process.{Environment.NewLine}";
        else
            contents += result.Result.ContinuationToken + Environment.NewLine;

        System.IO.File.WriteAllText(fn, contents);
    }
    else
        throw new Exception(result.ErrorMessage);

    Console.WriteLine(result.Successful ? $"Records Processed. {result.Result.Entries.Count} from {pageSize * pageCount} records in {sw.Elapsed.TotalSeconds} " : $"Error: {result.ErrorMessage}");
}

async Task GetTableSizesAsync(CancellationToken ct)
{
    var tableSizer = new TableSizer(syncSettings, _logger);

    var stats = await tableSizer.RunAsync(sampleSizePerTable: 500, maxConcurrency: 6, ct);
    foreach (var s in stats)
    {
        Console.WriteLine($"{s.Table,-40}\t\t\tcount={s.RowCount}, avg={s.AvgEntityBytes,8:0}B, {s.RowCount * s.AvgEntityBytes / (1024.0*1024.0)}mb");
    }
}

async Task PruneTableStorage(CancellationToken ct)
{
    var pruner = new TableStoragePruner(syncSettings, _logger);
    var options = new PruneOptions
    {   PruneEmptyTables = true,
        DryRun = true
    };

    var stats = await pruner.RunAsync(options, sampleSizePerTable: 500, maxConcurrency: 6, ct);
    var prunedcount = stats.Count(s => s.WouldDelete);
    Console.WriteLine($"Total tables that would be pruned: {prunedcount} total count {stats.Count}");
    foreach(var table in stats.Where(st=>st.WouldDelete))
    {
        Console.WriteLine($"Delete Table: {table.TableName} - {table.DeleteReason}? Y/N");
        if(Console.ReadKey().Key == ConsoleKey.Y)
        {
            var result = await pruner.DeleteTableAsync(table.TableName, ct);
            Console.WriteLine(result.Successful ? $"Deleted {table.TableName}" : $"Error deleting {table.TableName}: {result.ErrorMessage}");
        }
    }
}

async Task DeleteByEntityType(string entityType, int pageSize, int pageCount, bool dryRun, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();
    string contents = String.Empty;
    string token = null;
    var fn = $@"X:\DeleteByType_{entityType}.txt";
    if (System.IO.File.Exists(fn))
    {
        contents = System.IO.File.ReadAllText(fn);
        var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        token = lines.Last();
    }

    var result = await _syncRepo.DeleteByEntityTypeAsync(entityType, null, dryRun, pageSize, pageCount, ct);
    if (result.Successful)
    {
        contents += $"{DateTime.UtcNow.ToJSONString()} - Records Processed. {result.Result.DeletedCount}{Environment.NewLine}";
        if (String.IsNullOrEmpty(result.Result.ContinuationToken))
            contents += $"[COMPLETE] - {DateTime.UtcNow.ToJSONString()} - No more records to process.{Environment.NewLine}";
        else
            contents += result.Result.ContinuationToken + Environment.NewLine;
        System.IO.File.WriteAllText(fn, contents);
    }
    else
        throw new Exception(result.ErrorMessage);
    Console.WriteLine(result.Successful ? $"Records Processed. {result.Result.DeletedCount} from {pageSize * pageCount} records in {sw.Elapsed.TotalSeconds} " : $"Error: {result.ErrorMessage}");
}

async Task DeleteTableRows(string tableName, int pageSize,  bool dryRun, CancellationToken ct)
{
    var pruner = new TableStoragePruner(syncSettings, _logger);
    var sw = Stopwatch.StartNew();

    var cutoff = DateTimeOffset.UtcNow.AddMonths(-2);
    var cutoffRowKey = (DateTime.MaxValue.Ticks - cutoff.UtcDateTime.Ticks).ToString("D19");

    // older == RowKey > cutoffRowKey
    var filter = TableClient.CreateQueryFilter($"RowKey gt {cutoffRowKey}");

    await pruner.DeleteWhereAsync("errors", filter, dryRun, pageSize, ct);
}

HookShutdownSignals();


switch (mode)
{
    case "buildnodeindex":
        await BuildNodeLocatorIndexAsync(_shutdownCts.Token);
        break;
    case "tablesizes":
        await GetTableSizesAsync(_shutdownCts.Token);
        break;
    case "prune":
        await PruneTableStorage(_shutdownCts.Token);
        break;
    case "deletebytype":
        await DeleteByEntityType(entityType, pageSize, pageCount, dryRun,  _shutdownCts.Token);
        break;
    case "deletetablerows":
        await DeleteTableRows("errors", 10, false, _shutdownCts.Token);
        break;
}