
using Azure;
using Azure.Data.Tables;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Reader for the Node Locator table. Uses the same key derivation as the writer by
    /// relying on NodeLocatorTableEntityFactory for PK/RK.
    /// </summary>
    public sealed class NodeLocatorTableReader : INodeLocatorTableReader
    {
        private TableClient _nodeLocator;
        private readonly IAdminLogger _logger;
        private readonly string _accountKey;
        private readonly string _accountName;

        public NodeLocatorTableReader(IDefaultConnectionSettings connectionSettings, IAdminLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _accountName = connectionSettings?.DefaultTableStorageSettings?.AccountId;
            _accountKey = connectionSettings?.DefaultTableStorageSettings?.AccessKey;

            if (String.IsNullOrEmpty(_accountName)) throw new ArgumentNullException(nameof(_accountName));
            if (String.IsNullOrEmpty(_accountKey)) throw new ArgumentNullException(nameof(_accountKey));
        }

        private bool Initialized { get; set; }
        private DateTime? _initDate;

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

                _logger.Trace($"{this.Tag()} Was not initialized, table client created in {sw.ElapsedMilliseconds}ms");

                _initDate = DateTime.UtcNow.Date;
                Initialized = true;

                // For reads, we still want to ensure the table exists so we don’t get noisy failures.
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

        public async Task<NodeLocatorEntry> TryGetAsync(string nodeId, CancellationToken ct = default)
        {
            await EnsureCreatedAsync(ct);

            if (String.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("nodeId is required.", nameof(nodeId));

            await InitAsync(ct).ConfigureAwait(false);

            // Key derivation: use the same factory the writer uses so we never drift.
            // ToDeleteEntity should set PartitionKey + RowKey for the nodeId.
            string pk, rk;
            NodeLocatorKeys.GetKeys(nodeId, out pk, out rk);
            try
            {
                // Prefer "IfExists" so not-found does not throw.
                var response = await _nodeLocator
                    .GetEntityIfExistsAsync<TableEntity>(pk, rk, cancellationToken: ct)
                    .ConfigureAwait(false);

                if (!response.HasValue || response.Value == null)
                    return null;

                var tableEntity = response.Value;

                var entry = FromEntity(tableEntity);
                return entry;
            }
            catch (RequestFailedException rfe) when (rfe.Status == 404)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.AddException(this.Tag(), ex);
                throw;
            }
        }

        public static NodeLocatorEntry FromEntity(TableEntity e)
        {
            if (e == null) return null;

            // These property names are best-guess defaults. If your factory uses different names,
            // change them here (or, better, implement FromEntity inside the factory to match ToEntity).
            string GetString(string key)
            {
                return e.TryGetValue(key, out var v) ? v?.ToString() : null;
            }

            int GetInt(string key)
            {
                return e.TryGetValue(key, out var v) ? Convert.ToInt32(v?.ToString() ?? "-1") : -1;
            }

            DateTimeOffset? GetDateTimeOffset(string key)
            {
                if (!e.TryGetValue(key, out var v) || v == null) return null;
                if (v is DateTimeOffset dto) return dto;
                if (v is DateTime dt) return new DateTimeOffset(dt, TimeSpan.Zero);
                if (DateTimeOffset.TryParse(v.ToString(), out var parsed)) return parsed;
                return null;
            }

            // Common pattern: NodeId might be RowKey (or explicit property). Prefer explicit if present.
            var nodeId = GetString("NodeId");
            if (String.IsNullOrWhiteSpace(nodeId))
                nodeId = e.RowKey;

            return new NodeLocatorEntry
            {
                NodeId = nodeId,
                RootType = GetString(nameof(NodeLocatorEntry.RootType)),
                RootId = GetString(nameof(NodeLocatorEntry.RootId)),
                RootRevision = GetInt(nameof(NodeLocatorEntry.RootRevision)),
                RootLastUpdatedDate = GetString(nameof(NodeLocatorEntry.RootLastUpdatedDate)),
                RootOrgId = GetString(nameof(NodeLocatorEntry.RootOrgId)),
                NodePath = GetString(nameof(NodeLocatorEntry.NodePath)),
                NodeName = GetString(nameof(NodeLocatorEntry.NodeName)),
                NodeEntityType = GetString(nameof(NodeLocatorEntry.NodeEntityType)),
                NodeType = GetString(nameof(NodeLocatorEntry.NodeType)),
                SeenAt = GetDateTimeOffset(nameof(NodeLocatorEntry.SeenAt)) ?? default
            };
        }
    }
}

