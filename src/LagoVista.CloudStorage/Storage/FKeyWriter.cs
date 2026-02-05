using Azure;
using Azure.Data.Tables;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{

    public sealed class FkIndexTableWriterBatched : IFkIndexTableWriterBatched
    {
        private TableClient _inbound;
        private TableClient _outbound;
        private TableClient _orphaned;
        private readonly IAdminLogger _logger;
        private readonly string _accountKey;
        private readonly string _accountName;

        public FkIndexTableWriterBatched(IDefaultConnectionSettings connectionSettings, IAdminLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _accountName = connectionSettings.DefaultTableStorageSettings.AccountId;
            _accountKey =connectionSettings.DefaultTableStorageSettings.AccessKey;

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
                _inbound = serviceClient.GetTableClient(FkIndexKeys.InboundTable);
                _outbound = serviceClient.GetTableClient(FkIndexKeys.OutboundTable);
                _orphaned  = serviceClient.GetTableClient(FkIndexKeys.OrphandedTable);

                _logger.Trace($"{this.Tag()} Was not initialized, table created if didn't exist in {sw.ElapsedMilliseconds}ms");

                _initDate = DateTime.UtcNow.Date;
                Initialized = true;
                await EnsureCreatedAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
            }

        }

        private async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            await _orphaned.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
            await _inbound.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
            await _outbound.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
        }

        public async Task ApplyDiffAsync(FkEdgeDiff.DiffResult diff, CancellationToken ct = default)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));

            await InitAsync();

            // If you don't care about metadata refresh, skip SameIdentity.
            var added = diff.Added ?? new List<ForeignKeyEdge>();
            var removed = diff.Removed ?? new List<ForeignKeyEdge>();

            var inboundActions = BuildInboundActions(added, removed);
            var outboundActions = BuildOutboundActions(added, removed);

            await SubmitBatchesAsync(_outbound, outboundActions, ct).ConfigureAwait(false);
            await SubmitBatchesAsync(_inbound, inboundActions, ct).ConfigureAwait(false);
        }

        public async Task UpsertAllAsync(IEnumerable<ForeignKeyEdge> edges, CancellationToken ct = default)
        {
            var list = edges?.ToList() ?? new List<ForeignKeyEdge>();
            if (list.Count == 0) return;

            await InitAsync();

            var inboundActions = list.Select(e =>
            {
                var te = FkTableEntityFactory.ToInboundEntity(e);
                return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            });

            var outboundActions = list.Select(e =>
            {
                var te = FkTableEntityFactory.ToOutboundEntity(e);
                return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            });

            await SubmitBatchesAsync(_outbound, outboundActions, ct).ConfigureAwait(false);
            await SubmitBatchesAsync(_inbound, inboundActions, ct).ConfigureAwait(false);
        }

        public async Task AddOrphanedEHAsync(IEntityBase entity, string path, EntityHeader eh, CancellationToken ct = default)
        {
            var te = FkTableEntityFactory.ToOrphanedEntity(entity, path, eh);
            var tableTx = new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            await _orphaned.UpsertEntityAsync(te, TableUpdateMode.Replace, ct);
        }

        private static IEnumerable<TableTransactionAction> BuildOutboundActions(
            IEnumerable<ForeignKeyEdge> added,
            IEnumerable<ForeignKeyEdge> removed)
        {
            foreach (var e in added)
            {
                var te = FkTableEntityFactory.ToOutboundEntity(e);
                yield return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            }

            foreach (var e in removed)
            {
                string pk, rk;
                FkIndexKeys.GetOutboundKeys(e, out pk, out rk);
                var te = new TableEntity(pk, rk) { ETag = ETag.All };
                yield return new TableTransactionAction(TableTransactionActionType.Delete, te);
            }
        }

        private static IEnumerable<TableTransactionAction> BuildInboundActions(
            IEnumerable<ForeignKeyEdge> added,
            IEnumerable<ForeignKeyEdge> removed)
        {
            foreach (var e in added)
            {
                var te = FkTableEntityFactory.ToInboundEntity(e);
                yield return new TableTransactionAction(TableTransactionActionType.UpsertReplace, te);
            }

            foreach (var e in removed)
            {
                string pk, rk;
                FkIndexKeys.GetInboundKeys(e, out pk, out rk);
                var te = new TableEntity(pk, rk) { ETag = ETag.All };
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



    public static class FkTableEntityFactory
    {
        public static TableEntity ToInboundEntity(ForeignKeyEdge e)
        {
            e.EnsurePathHash();
            string pk, rk;
            FkIndexKeys.GetInboundKeys(e, out pk, out rk);

            return BuildEntity(pk, rk, e);
        }

        public static TableEntity ToOrphanedEntity(IEntityBase entityBase, string path, EntityHeader eh)
        {
            var pk = entityBase.OwnerOrganization == null ? $"SYSTEM|{entityBase.EntityType}":  $"{entityBase.OwnerOrganization.Id}|{entityBase.EntityType}";
            var rk = $"{entityBase.Id}";
            var te = new TableEntity(pk, rk);
            te["EntityName"]  = entityBase.Name;
            te["EntityId"] = entityBase.Id;
            te["EntityCreationDate"] = entityBase.CreationDate;
            te["EntityLastUpdatedDate"] = entityBase.LastUpdatedDate;
            te["OrphanedAt"] = DateTime.UtcNow.ToJSONString();
            te["OrgId"] = entityBase.OwnerOrganization?.Id ?? "SYSTEM";
            te["Org"] = entityBase.OwnerOrganization?.Text ?? "SYSTEM";
            te["EhPath"] = path;
            te["OrphanedId"] = eh.Id;
            te["OrphanedKey"] = String.IsNullOrEmpty(eh.Key) ? "-" : eh.Key;
            te["OrphanedText"] = eh.Text;
            return te;
        }

        public static TableEntity ToOutboundEntity(ForeignKeyEdge e)
        {
            e.EnsurePathHash();
            string pk, rk;
            FkIndexKeys.GetOutboundKeys(e, out pk, out rk);

            return BuildEntity(pk, rk, e);
        }

        private static TableEntity BuildEntity(string pk, string rk, ForeignKeyEdge e)
        {
            var te = new TableEntity(pk, rk);

            te["SrcOrgId"] = e.Source.OrgId;
            te["SrcType"] = e.Source.EntityType;
            te["SrcId"] = e.Source.EntityId;

            te["DstOrgId"] = e.Target.OrgId;
            te["DstType"] = e.Target.EntityType;
            te["DstId"] = e.Target.EntityId;

            te["RefPath"] = e.RefPath;
            te["RefPathHash"] = e.RefPathHash;

            // Optional metadata
            te["DstKey"] = e.TargetKey;
            te["DstText"] = e.TargetText;

            te["SeenAt"] = e.SeenAt.UtcDateTime;

            if (e.SourceRevision.HasValue)
                te["SrcRevision"] = e.SourceRevision.Value;

            if (!string.IsNullOrWhiteSpace(e.SourceLastUpdatedDate))
                te["SrcLastUpdatedDate"] = e.SourceLastUpdatedDate;

            return te;
        }
    }
}
