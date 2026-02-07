using Azure;
using Azure.Data.Tables;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Utils;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public interface INodeLocatorTableWriterBatched
    {
        Task ApplyDiffAsync(NodeLocatorDiff.DiffResult diff, CancellationToken ct = default);
        Task UpsertAllAsync(IEnumerable<NodeLocatorEntry> entries, CancellationToken ct = default);
        Task EnsureCreatedAsync(CancellationToken ct = default);
    }

    public sealed class NodeLocatorTableWriterBatched : INodeLocatorTableWriterBatched
    {
        private TableClient _nodeLocator;
        private readonly IAdminLogger _logger;
        private readonly string _accountKey;
        private readonly string _accountName;

        public NodeLocatorTableWriterBatched(IDefaultConnectionSettings connectionSettings, IAdminLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _accountName = connectionSettings.DefaultTableStorageSettings.AccountId;
            _accountKey = connectionSettings.DefaultTableStorageSettings.AccessKey;

            if (String.IsNullOrEmpty(_accountName)) throw new ArgumentNullException(nameof(_accountName));
            if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException(nameof(_accountKey));
        }

        private bool Initialized { get; set; }
        DateTime? _initDate;

        private async Task InitAsync(CancellationToken ct = default)
        {
            lock (this)
            {
                if (Initialized && _initDate.HasValue && _initDate == DateTime.UtcNow.Date)
                {
                    return;
                }
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountName};AccountKey={_accountKey}";

                var serviceClient = new TableServiceClient(connectionString);
                _nodeLocator = serviceClient.GetTableClient(NodeLocatorKeys.NodeLocatorTable);

                _logger.Trace($"{this.Tag()} Was not initialized, table created if didn't exist in {sw.ElapsedMilliseconds}ms");

                _initDate = DateTime.UtcNow.Date;
                Initialized = true;
                await EnsureCreatedAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
            }
        }

        public async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            await InitAsync(ct).ConfigureAwait(false);
            await _nodeLocator.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
        }

        public async Task ApplyDiffAsync(NodeLocatorDiff.DiffResult diff, CancellationToken ct = default)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));
            await InitAsync(ct).ConfigureAwait(false);

            var upserts = diff.Upserts ?? new List<NodeLocatorEntry>();
            var deletes = diff.Deletes ?? new List<NodeLocatorEntry>();

            var actions = BuildActions(upserts, deletes);

            await SubmitBatchesAsync(_nodeLocator, actions, ct).ConfigureAwait(false);
        }

        public sealed class DuplicateNodeId
        {
            public string NodeId { get; set; }
            public int Count { get; set; }
            public List<string> Paths { get; set; } = new List<string>();
            public List<string> NodeTypes { get; set; } = new List<string>();
        }

        public static List<NodeLocatorEntry> DeduplicateByNodeId(
        IEnumerable<NodeLocatorEntry> entries,
        string rootId)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var rootIdNorm = rootId;

            return entries
                .Where(e => e != null && !String.IsNullOrWhiteSpace(e.NodeId))
                .GroupBy(e => e.NodeId, StringComparer.Ordinal)
                .Select(g => PickBest(g, rootIdNorm))
                .ToList();
        }

        private static NodeLocatorEntry PickBest(
            IGrouping<string, NodeLocatorEntry> group,
            string rootId)
        {
            // Deterministic ordering: "best" first
            return group
                .OrderByDescending(e => IsAuthoritativeRoot(e, rootId))         // keep "/" only when this NodeId is the rootId
                .ThenByDescending(e => HasRealType(e))                          // prefer NodeType != "-"
                .ThenBy(e => IsRootPath(e) ? 1 : 0)                             // otherwise prefer non-root paths
                .ThenBy(e => PathLength(e))                                     // prefer shorter path
                .ThenByDescending(e => e.SeenAt)                                // newest wins last tie
                .First();
        }

        private static bool IsAuthoritativeRoot(NodeLocatorEntry e, string rootId)
        {
            // If this node is the root entity, "/" is the authoritative path.
            return !String.IsNullOrWhiteSpace(rootId)
                && String.Equals(e.NodeId, rootId, StringComparison.Ordinal)
                && String.Equals(e.NodePath, "/", StringComparison.Ordinal);
        }

        private static bool HasRealType(NodeLocatorEntry e)
        {
            return !String.IsNullOrWhiteSpace(e.NodeType)
                && !String.Equals(e.NodeType, "-", StringComparison.Ordinal);
        }

        private static bool IsRootPath(NodeLocatorEntry e)
        {
            return String.IsNullOrWhiteSpace(e.NodePath)
                || String.Equals(e.NodePath, "/", StringComparison.Ordinal);
        }

        private static int PathLength(NodeLocatorEntry e)
        {
            return String.IsNullOrWhiteSpace(e.NodePath) ? Int32.MaxValue : e.NodePath.Length;
        }

        public static List<DuplicateNodeId> FindDuplicateNodeIds(IEnumerable<NodeLocatorEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            return entries
                .Where(e => e != null && !String.IsNullOrWhiteSpace(e.NodeId))
                .GroupBy(e => e.NodeId, StringComparer.Ordinal) // strict
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateNodeId
                {
                    NodeId = g.Key,
                    Count = g.Count(),
                    Paths = g.Select(x => x.NodePath).Where(p => !String.IsNullOrWhiteSpace(p)).Distinct().ToList(),
                    NodeTypes = g.Select(x => x.NodeType).Where(t => !String.IsNullOrWhiteSpace(t)).Distinct().ToList(),
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.NodeId)
                .ToList();
        }

        public static string FormatDuplicates(IEnumerable<DuplicateNodeId> dups)
        {
            if (dups == null) return "(none)";

            var sb = new StringBuilder();
            foreach (var d in dups)
            {
                sb.AppendLine($"NodeId: {d.NodeId}  Count: {d.Count}");

                if (d.NodeTypes.Count > 0)
                    sb.AppendLine("  Types: " + String.Join(", ", d.NodeTypes));

                foreach (var p in d.Paths)
                    sb.AppendLine("  Path: " + p);

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public async Task UpsertAllAsync(IEnumerable<NodeLocatorEntry> entries, CancellationToken ct = default)
        {
            var list = entries?.ToList() ?? new List<NodeLocatorEntry>();
            if (list.Count == 0) return;

            await InitAsync(ct).ConfigureAwait(false);

            var actions = list.Select(e =>
            {
                var te = NodeLocatorTableEntityFactory.ToEntity(e);
                return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            });

            try
            {
                await SubmitBatchesAsync(_nodeLocator, actions, ct).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        private static IEnumerable<TableTransactionAction> BuildActions(
            IEnumerable<NodeLocatorEntry> upserts,
            IEnumerable<NodeLocatorEntry> deletes)
        {
            foreach (var e in upserts)
            {
                var te = NodeLocatorTableEntityFactory.ToEntity(e);
                yield return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            }

            foreach (var e in deletes)
            {
                if (e == null || String.IsNullOrWhiteSpace(e.NodeId)) continue;

                var te = NodeLocatorTableEntityFactory.ToDeleteEntity(e.NodeId);
                yield return new TableTransactionAction(TableTransactionActionType.Delete, te);
            }
        }

        private static async Task SubmitBatchesAsync(
            TableClient client,
            IEnumerable<TableTransactionAction> actions,
            CancellationToken ct)
        {
            var grouped = actions.GroupBy(a => a.Entity.PartitionKey);

            foreach (var g in grouped)
            {
                var batch = new List<TableTransactionAction>(100);

                foreach (var a in g)
                {
                    batch.Add(a);

                    if (batch.Count == 100)
                    {
                        await client.SubmitTransactionAsync(batch, ct).ConfigureAwait(false);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                    await client.SubmitTransactionAsync(batch, ct).ConfigureAwait(false);
            }
        }
    }
}
