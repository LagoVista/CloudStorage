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
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using MongoDB.Bson.Serialization.IdGenerators;
using Moq;
using System.Diagnostics;
using System.Security.Cryptography;
using ZstdSharp.Unsafe;
using static System.Net.WebRequestMethods;

ISyncRepository _syncRepo;
IFkIndexTableWriterBatched _fkeyWriter;
INodeLocatorTableWriterBatched _nodeWriter;
INodeLocatorTableReader _nodeReader;
IAdminLogger _logger;
ISyncConnectionSettings _syncSettings;
IDefaultConnectionSettings _defaultConnetionSettings;

CancellationTokenSource _shutdownCts;

const int defaultPageSize = 500;
const int defaultPageCount = 10;

string _env;

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

void Init(string env)
{
    _env = env;
    var syncSettings = new SyncSettings(env);
    _syncSettings = syncSettings;
    _defaultConnetionSettings = syncSettings;

    _logger = new AdminLogger(new ConsoleLogWriter());
    _fkeyWriter = new FkIndexTableWriterBatched(syncSettings, _logger);
    _nodeWriter = new NodeLocatorTableWriterBatched(syncSettings, _logger);
    _nodeReader = new NodeLocatorTableReader(syncSettings, _logger);
    _syncRepo = new CosmosSyncRepository(syncSettings, _fkeyWriter, _nodeWriter, _nodeReader, new Mock<ICacheProvider>().Object, new AdminLogger(new ConsoleLogWriter()));

    _shutdownCts = new CancellationTokenSource();

    HookShutdownSignals();
}



async Task BuildNodeLocatorIndexAsync(int pageSize = defaultPageSize, int pageCount = defaultPageCount, CancellationToken ct = default)
{
    var sw = Stopwatch.StartNew();

    string contents = String.Empty;
    string token = null;
    var fn = @$"X:\Nodes-{_env}.txt";
    if (System.IO.File.Exists(fn))
    {
        contents = System.IO.File.ReadAllText(fn);
        var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        token = lines.Last();
    }

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

async Task GetTableSizesAsync(CancellationToken ct = default)
{
    var tableSizer = new TableSizer(_defaultConnetionSettings, _logger);

    var stats = await tableSizer.RunAsync(sampleSizePerTable: 500, maxConcurrency: 6, ct);
    var builder = new System.Text.StringBuilder();

    foreach (var s in stats)
    {
        builder.AppendLine($"{s.Table},{s.RowCount},{s.AvgEntityBytes,8:0}B,{s.RowCount * s.AvgEntityBytes / (1024.0*1024.0)}mb");
        Console.WriteLine($"{s.Table,-40}\t\t\tcount={s.RowCount}, avg={s.AvgEntityBytes,8:0}B, {s.RowCount * s.AvgEntityBytes / (1024.0*1024.0)}mb");
    }

    System.IO.File.WriteAllText($@"X:\TableSizes-{_env}.csv", builder.ToString());
}

async Task PruneTableStorage(CancellationToken ct = default)
{
    var pruner = new TableStoragePruner(_defaultConnetionSettings, _logger);
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

async Task ResolveFKeysAsync(int pageSize = defaultPageSize, int pageCount = defaultPageCount, bool dryRun = false, CancellationToken ct = default)
{
    string contents = String.Empty;
    string token = null;
    var fn = @$"X:\FKeyResolver-{_env}.txt";
    if (System.IO.File.Exists(fn))
    {
        contents = System.IO.File.ReadAllText(fn);
        var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        token = lines.Last();
    }

    int idx = 0;
    var result = await _syncRepo.ScanContainerAsync(async (row, ct) =>
    {
        await _syncRepo.ResolveEntityHeadersAsync(row.Id, dryRun: false);
        idx++;
    }, token, null, pageSize, pageCount, null);

    
    contents += $"{DateTime.UtcNow.ToJSONString()} - Records Processed. {idx}{Environment.NewLine}";
    contents += result + Environment.NewLine;
    System.IO.File.WriteAllText(fn, contents);
}

async Task DeleteByEntityType(string entityType, int pageSize = defaultPageSize, int pageCount = defaultPageCount, bool dryRun = false, CancellationToken ct = default)
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
    var pruner = new TableStoragePruner(_defaultConnetionSettings, _logger);
    var sw = Stopwatch.StartNew();

    var cutoff = DateTimeOffset.UtcNow.AddMonths(-2);
    var cutoffRowKey = (DateTime.MaxValue.Ticks - cutoff.UtcDateTime.Ticks).ToString("D19");

    // older == RowKey > cutoffRowKey
    var filter = TableClient.CreateQueryFilter($"RowKey gt {cutoffRowKey}");

    await pruner.DeleteWhereAsync("errors", filter, dryRun, pageSize, ct);
}

string GetCacheKey(string dbName, string entityType, string id)
{
    return $"{dbName}-{entityType}-{id}".ToLower();
}

async Task SetEntityHashAsync(string? entityType = null, int pageSize = defaultPageSize, int pageCount = defaultPageCount, CancellationToken ct = default)
{
    var http = new HttpClient();

    string contents = String.Empty;
    string token = null;
    var fn = @$"X:\HashSet-{_env}.txt";
    if (System.IO.File.Exists(fn))
    {
        contents = System.IO.File.ReadAllText(fn);
        var lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        token = lines.Last();
    }

    if (token != null && token.StartsWith("[COMPLETED]"))
    {
        Console.WriteLine("Done.");
        return;
    }

    int idx = 0;
    var result = await _syncRepo.ScanContainerAsync(async (row, ct) =>
    {
        var cacheKey = GetCacheKey(_syncSettings.SyncConnectionSettings.ResourceName, row.EntityType, row.Id);  

        var sw = Stopwatch.StartNew();
        var result = await _syncRepo.SetEntityHashAsync(row.Id);
        if (result.Successful)
        {
            var hostName = _env == "prod" ? "www" : "dev";   
            await http.GetAsync($"https://{hostName}.nuviot.com/api/core/cache/clear/{cacheKey}", ct).ConfigureAwait(false);
            Console.WriteLine($"Updated {++idx:00000} of {pageSize * pageCount} in {sw.Elapsed.TotalMilliseconds}ms");
        }
        else
            Console.WriteLine($"Error updating {++idx} {result.ErrorMessage} in {sw.Elapsed.TotalMilliseconds}ms");

    }, token, entityType, pageSize, pageCount, null);


    contents += $"{DateTime.UtcNow.ToJSONString()} - Records Processed. {idx}{Environment.NewLine}";
    if (String.IsNullOrEmpty(result))
        contents += "[COMPLETED]";
    else
        contents += result + Environment.NewLine;
    System.IO.File.WriteAllText(fn, contents);
}


var mode = "sethash";
var env = "prod";
var entityType = "ExternalWorkTask";
var dryRun = false;

Init(env);

switch (mode)
{
    case "resolvefkeys":
        await ResolveFKeysAsync(dryRun:dryRun, ct:_shutdownCts.Token);
        break;
    case "buildnodeindex":
        await BuildNodeLocatorIndexAsync(ct:_shutdownCts.Token);
        break;
    case "tablesizes":
        await GetTableSizesAsync(_shutdownCts.Token);
        break;
    case "prune":
        await PruneTableStorage(_shutdownCts.Token);
        break;
    case "deletebytype":
        await DeleteByEntityType(entityType, dryRun:dryRun,  ct:_shutdownCts.Token);
        break;
    case "deletetablerows":
        await DeleteTableRows("errors", 10, false, _shutdownCts.Token);
        break;
    case "sethash":
        await SetEntityHashAsync("Module");
        break;
}