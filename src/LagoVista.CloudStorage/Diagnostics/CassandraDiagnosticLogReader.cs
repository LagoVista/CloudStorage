using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>Bounded application/environment/time-scoped queries; no global scans.</summary>
    public sealed class CassandraDiagnosticLogReader : IDiagnosticLogReader
    {
        private readonly IActivityRecordStore<DiagnosticLogActivityRecord> _store;

        public CassandraDiagnosticLogReader(IActivityRecordStore<DiagnosticLogActivityRecord> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.ToUniversalTime();
        }

        public async Task<DiagnosticLogPage> QueryAsync(DiagnosticLogQuery request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrWhiteSpace(request.Application)) throw new ArgumentException("Application is required.", nameof(request));
            if (String.IsNullOrWhiteSpace(request.Environment)) throw new ArgumentException("Environment is required.", nameof(request));
            if (request.StartUtc == default(DateTime) || request.EndUtc == default(DateTime))
                throw new ArgumentException("StartUtc and EndUtc are required.", nameof(request));

            var start = NormalizeUtc(request.StartUtc);
            var end = NormalizeUtc(request.EndUtc);
            if (end < start || end - start > TimeSpan.FromHours(48))
                throw new ArgumentException("Diagnostic queries must have a valid window of at most 48 hours.", nameof(request));
            if (request.PageSize < 1 || request.PageSize > 500)
                throw new ArgumentOutOfRangeException(nameof(request), "PageSize must be between 1 and 500.");

            var query = new HistoryQuery<DiagnosticLogActivityRecord>()
                .Between(start, end)
                .Where(x => x.Application, StorageFilterOperator.Equal, request.Application.Trim())
                .Where(x => x.Environment, StorageFilterOperator.Equal, request.Environment.Trim())
                .WithPage(new StoragePageRequest(request.PageSize, request.ContinuationToken));

            if (!String.IsNullOrWhiteSpace(request.Level))
                query.Where(x => x.LogLevel, StorageFilterOperator.Equal, request.Level);
            if (!String.IsNullOrWhiteSpace(request.Tag))
                query.Where(x => x.Tag, StorageFilterOperator.Equal, request.Tag);

            var page = await _store.QueryAsync(query, cancellationToken).ConfigureAwait(false);
            return new DiagnosticLogPage
            {
                Records = page.Items.Select(item => item.ToLogRecord()).ToList(),
                ContinuationToken = page.ContinuationToken
            };
        }
    }
}
