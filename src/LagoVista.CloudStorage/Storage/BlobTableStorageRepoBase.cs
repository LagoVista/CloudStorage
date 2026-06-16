using Azure.Data.Tables;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    public abstract class BlobTableStorageRepoBase<TDetail, TSummary>
        where TDetail : class, IStorageIdProvider, IOwnedStorageRecord, ISummaryTableBuilder<TSummary>
        where TSummary : class, new()
    {
        private readonly string _accountId;
        private readonly string _accessKey;
        private readonly IAdminLogger _logger;

        private CloudFileStorage _fileStorage;
        private TableClient _summaryTableClient;
        private bool _initialized;

        protected BlobTableStorageRepoBase(string accountId, string accessKey, IAdminLogger logger)
        {
            if (String.IsNullOrWhiteSpace(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (String.IsNullOrWhiteSpace(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            _accountId = accountId;
            _accessKey = accessKey;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected virtual string GetStorageName()
        {
            return typeof(TDetail).Name;
        }

        protected virtual string GetDetailBlobContainerName()
        {
            return StorageNameUtility.ToBlobContainerName(GetStorageName());
        }

        protected virtual string GetSummaryTableName()
        {
            return StorageNameUtility.ToTableName(GetStorageName());
        }

        protected virtual string GetPartitionKey(TDetail detail)
        {
            return detail.OwnerOrganization.Id;
        }

        protected virtual string GetRowKey(TDetail detail)
        {
            return detail.Id.Value;
        }

        protected virtual bool ShouldWriteArchiveRecord(TDetail detail)
        {
            return false;
        }

        protected virtual string GetArchivePartitionKey(TDetail detail)
        {
            return $"{GetPartitionKey(detail)}_{GetRowKey(detail)}";
        }

        protected virtual string GetArchiveRowKey(TDetail detail)
        {
            return ReverseTicks();
        }

        protected virtual string GetDetailBlobName(string orgId, string id)
        {
            return $"{orgId}/{id}.json";
        }

        protected virtual string GetDetailBlobName(TDetail detail)
        {
            return GetDetailBlobName(detail.OwnerOrganization.Id, detail.Id.Value);
        }

        protected virtual string GetArchiveDetailBlobName(TDetail detail, string archiveRowKey)
        {
            return $"{detail.OwnerOrganization.Id}/{detail.Id.Value}/archive/{archiveRowKey}.json";
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_accountId};AccountKey={_accessKey}";

            _fileStorage = new CloudFileStorage(_accountId, _accessKey, _logger);
            _summaryTableClient = new TableClient(connectionString, GetSummaryTableName());

            await _summaryTableClient.CreateIfNotExistsAsync();

            _initialized = true;
        }

        protected async Task<TSummary> GetSummaryByRowKeyAsync(string rowKey)
        {
            if (String.IsNullOrWhiteSpace(rowKey))
                throw new ArgumentNullException(nameof(rowKey));

            await EnsureInitializedAsync();

            var filter = TableStorageQuery.Create()
                .WhereEquals("RowKey", rowKey)
                .ToFilterString();

            await foreach (var entity in _summaryTableClient.QueryAsync<TableEntity>(filter: filter, maxPerPage: 1))
            {
                return CreateSummaryFromTableEntity(entity);
            }

            return null;
        }

        protected async Task<InvokeResult> SaveAsync(TDetail detail)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            if (detail.Id == null) throw new InvalidOperationException($"{typeof(TDetail).Name}.Id is required.");
            if (detail.OwnerOrganization == null) throw new InvalidOperationException($"{typeof(TDetail).Name}.OwnerOrganization is required.");
            if (String.IsNullOrWhiteSpace(detail.OwnerOrganization.Id)) throw new InvalidOperationException($"{typeof(TDetail).Name}.OwnerOrganization.Id is required.");

            await EnsureInitializedAsync();

            var detailJson = JsonConvert.SerializeObject(detail);

            var currentBlobResult = await _fileStorage.AddFileAsync(
                GetDetailBlobContainerName(),
                GetDetailBlobName(detail),
                detailJson,
                "application/json");

            if (!currentBlobResult.Successful)
                return currentBlobResult.ToInvokeResult();

            var summary = detail.CreateSummary();
            if (summary == null)
                throw new InvalidOperationException($"{typeof(TDetail).Name}.CreateSummary returned null.");

            var currentTableEntity = CreateSummaryTableEntity(
                GetPartitionKey(detail),
                GetRowKey(detail),
                summary);

            await _summaryTableClient.UpsertEntityAsync(currentTableEntity, TableUpdateMode.Replace);

            if (!ShouldWriteArchiveRecord(detail))
                return InvokeResult.Success;

            var archiveRowKey = GetArchiveRowKey(detail);

            var archiveBlobResult = await _fileStorage.AddFileAsync(
                GetDetailBlobContainerName(),
                GetArchiveDetailBlobName(detail, archiveRowKey),
                detailJson,
                "application/json");

            if (!archiveBlobResult.Successful)
                return archiveBlobResult.ToInvokeResult();

            var archiveTableEntity = CreateSummaryTableEntity(
                GetArchivePartitionKey(detail),
                archiveRowKey,
                summary);

            await _summaryTableClient.UpsertEntityAsync(archiveTableEntity, TableUpdateMode.Replace);

            return InvokeResult.Success;
        }

        protected async Task<TDetail> GetDetailAsync(string orgId, string id)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            await EnsureInitializedAsync();

            var blobName = GetDetailBlobName(orgId, id);
            var blobResult = await _fileStorage.GetFileAsync(GetDetailBlobContainerName(), blobName);

            if (!blobResult.Successful)
            {
                var error = blobResult.Errors.FirstOrDefault()?.Message ?? $"Could not load detail blob [{blobName}].";
                throw new InvalidOperationException(error);
            }

            var json = Encoding.UTF8.GetString(blobResult.Result);
            return JsonConvert.DeserializeObject<TDetail>(json);
        }

        protected async Task<TDetail> GetArchiveDetailAsync(string orgId, string id, string archiveRowKey)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrWhiteSpace(archiveRowKey)) throw new ArgumentNullException(nameof(archiveRowKey));

            await EnsureInitializedAsync();

            var blobName = $"{orgId}/{id}/archive/{archiveRowKey}.json";
            var blobResult = await _fileStorage.GetFileAsync(GetDetailBlobContainerName(), blobName);

            if (!blobResult.Successful)
            {
                var error = blobResult.Errors.FirstOrDefault()?.Message ?? $"Could not load archive detail blob [{blobName}].";
                throw new InvalidOperationException(error);
            }

            var json = Encoding.UTF8.GetString(blobResult.Result);
            return JsonConvert.DeserializeObject<TDetail>(json);
        }

        protected async Task<ListResponse<TSummary>> GetSummariesAsync(
            string orgId,
            ListRequest listRequest,
            TableStorageQuery query = null)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (listRequest == null) listRequest = new ListRequest { PageSize = 100 };

            await EnsureInitializedAsync();

            var filter = TableStorageQuery.Create()
                .WhereEquals("PartitionKey", orgId)
                .And(query)
                .ToFilterString();

            _logger.Trace($"{this.Tag()} - {filter}");

            var pageSize = Math.Min(1000, listRequest.PageSize <= 0 ? 100 : listRequest.PageSize);
            var rows = new List<TSummary>();

            await foreach (var entity in _summaryTableClient.QueryAsync<TableEntity>(
                filter: filter,
                maxPerPage: pageSize))
            {
                rows.Add(CreateSummaryFromTableEntity(entity));

                if (rows.Count >= pageSize)
                    break;
            }

            _logger.Trace($"{this.Tag()} - return {rows.Count}");

            return ListResponse<TSummary>.Create(rows);
        }

        protected async Task<ListResponse<TSummary>> GetArchiveSummariesAsync(
            string orgId,
            string id,
            ListRequest listRequest,
            TableStorageQuery query = null)
        {
            if (String.IsNullOrWhiteSpace(orgId)) throw new ArgumentNullException(nameof(orgId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (listRequest == null) listRequest = new ListRequest { PageSize = 100 };

            await EnsureInitializedAsync();

            var archivePartitionKey = $"{orgId}_{id}";

            var filter = TableStorageQuery.Create()
                .WhereEquals("PartitionKey", archivePartitionKey)
                .And(query)
                .ToFilterString();

            _logger.Trace($"{this.Tag()} - {filter}");

            var pageSize = Math.Min(1000, listRequest.PageSize <= 0 ? 100 : listRequest.PageSize);
            var rows = new List<TSummary>();

            await foreach (var entity in _summaryTableClient.QueryAsync<TableEntity>(
                filter: filter,
                maxPerPage: pageSize))
            {
                rows.Add(CreateSummaryFromTableEntity(entity));

                if (rows.Count >= pageSize)
                    break;
            }

            _logger.Trace($"{this.Tag()} - return archive {rows.Count}");

            return ListResponse<TSummary>.Create(rows);
        }

        private TableEntity CreateSummaryTableEntity(string partitionKey, string rowKey, TSummary summary)
        {
            if (String.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentNullException(nameof(partitionKey));
            if (String.IsNullOrWhiteSpace(rowKey)) throw new ArgumentNullException(nameof(rowKey));
            if (summary == null) throw new ArgumentNullException(nameof(summary));

            var entity = new TableEntity(partitionKey, rowKey);

            foreach (var property in typeof(TSummary).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                if (IsReservedTableProperty(property.Name))
                    continue;

                var value = property.GetValue(summary);
                if (value == null)
                    continue;

                SummaryTablePropertyWriter.Add(entity, property.Name, value);
            }

            return entity;
        }

        private TSummary CreateSummaryFromTableEntity(TableEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var summary = new TSummary();

            foreach (var property in typeof(TSummary).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanWrite)
                    continue;

                if (IsReservedTableProperty(property.Name))
                    continue;

                if (SummaryTablePropertyReader.TrySetEntityHeader(summary, property, entity))
                    continue;

                if (!entity.TryGetValue(property.Name, out var value))
                    continue;

                SummaryTablePropertyReader.Set(summary, property, value);
            }

            return summary;
        }

        private static bool IsReservedTableProperty(string propertyName)
        {
            return propertyName == "PartitionKey" ||
                   propertyName == "RowKey" ||
                   propertyName == "Timestamp" ||
                   propertyName == "ETag";
        }

        private static string ReverseTicks()
        {
            return $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}";
        }
    }
}