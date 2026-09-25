using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>
    /// Mongo Scratch-backed diagnostic repository for bounded application errors.
    /// Records are intentionally system scoped and retained by Scratch TTL policy.
    /// </summary>
    public sealed class ScratchApplicationErrorRepository : IApplicationErrorRepository
    {
        private static readonly EntityHeader SystemOrganization =
            EntityHeader.Create("NUVIOT-DIAGNOSTICS", "NuvIoT Diagnostics");

        private readonly IScratchStore _store;

        public ScratchApplicationErrorRepository(IScratchStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Task WriteErrorAsync(LogRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var scratchRecord = new ApplicationErrorScratchRecord
            {
                Id = record.Id,
                Organization = SystemOrganization,
                Record = record
            };

            return _store.UpsertAsync(scratchRecord, cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationErrorSummary>> GetRecentErrorsAsync(
            int take = 100,
            string application = null,
            CancellationToken cancellationToken = default)
        {
            if (take <= 0) take = 100;
            if (take > 1000) take = 1000;

            var query = new StorageQuery<ApplicationErrorScratchRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, SystemOrganization.Id)
                .OrderBy(x => x.Record.TimeStamp, StorageSortDirection.Descending)
                .WithPage(new StoragePageRequest(take));

            if (!String.IsNullOrWhiteSpace(application))
            {
                query.Where(x => x.Record.Application, StorageFilterOperator.Equal, application.Trim());
            }


            var page = await _store.QueryAsync(query, cancellationToken).ConfigureAwait(false);

            return page.Items
                .Where(item => item?.Record != null)
                .Select(item => new ApplicationErrorSummary
                {
                    Id = item.Record.Id,
                    TimeStamp = item.Record.TimeStamp,
                    Application = item.Record.Application,
                    Environment = item.Record.Environment,
                    LogLevel = item.Record.LogLevel,
                    Tag = item.Record.Tag,
                    Message = item.Record.Message,
                    ExceptionType = item.Record.ExceptionType,
                    Version = item.Record.Version,
                    HostId = item.Record.HostId
                })
                .ToList();
        }

        public async Task<LogRecord> GetErrorAsync(string id, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            var record = await _store.GetAsync<ApplicationErrorScratchRecord>(
                new StorageKey(id.Trim(), SystemOrganization.Id),
                cancellationToken).ConfigureAwait(false);

            return record?.Record;
        }
    }
}
