using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Logging.Utils;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>
    /// Opt-in Cassandra diagnostic sink. Console output stays available if Cassandra is down;
    /// persistence failures never recurse into the application logger.
    /// </summary>
    public sealed class CassandraLogWriter : ILogWriter
    {
        private readonly IActivityRecordStore<DiagnosticLogActivityRecord> _store;
        private readonly IRuntimeControlService _controls;
        private readonly ConsoleLogWriter _console = new ConsoleLogWriter();
        private readonly bool _writeConsole;
        private int _firstPersisted;
        private int _reportedPersistenceFailure;
        private readonly string _application;
        private readonly string _environment;
        private readonly string _version;

        public CassandraLogWriter(
            IActivityRecordStore<DiagnosticLogActivityRecord> store,
            IRuntimeControlService controls,
            string application,
            string environment,
            string version = null,
            bool writeConsole = true)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _controls = controls;
            _application = !String.IsNullOrWhiteSpace(application) ? application : throw new ArgumentNullException(nameof(application));
            _environment = !String.IsNullOrWhiteSpace(environment) ? environment : throw new ArgumentNullException(nameof(environment));
            _version = version;
            _writeConsole = writeConsole;
        }

        public Task WriteError(LogRecord record) => WriteAsync(record, isError: true);
        public Task WriteEvent(LogRecord record) => WriteAsync(record, isError: false);

        private async Task WriteAsync(LogRecord record, bool isError)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (_writeConsole)
            {
                try
                {
                    if (isError) await _console.WriteError(record).ConfigureAwait(false);
                    else await _console.WriteEvent(record).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[CassandraLogWriter__Console] {ex.GetType().Name}: {ex.Message}");
                }
            }

            // Scoped diagnostics are independent of the global verbose switch.
            // Errors and non-verbose events are never gated here.
            if (!isError && IsVerbose(record.LogLevel))
            {
                var controlKey = RuntimeControlCatalog.LoggingVerbose;
                if (String.Equals(record.Tag, RuntimeControlCatalog.JobManagerDiagnostics, StringComparison.OrdinalIgnoreCase))
                    controlKey = RuntimeControlCatalog.JobManagerDiagnostics;
                else if (String.Equals(record.Tag, RuntimeControlCatalog.S3FileStorageDiagnostics, StringComparison.OrdinalIgnoreCase))
                    controlKey = RuntimeControlCatalog.S3FileStorageDiagnostics;

                if (_controls?.IsEnabled(controlKey) != true)
                    return;
            }

            try
            {
                var activity = DiagnosticLogActivityRecord.FromLogRecord(record, _application, _environment, _version);
                await _store.InsertAsync(activity).ConfigureAwait(false);
                Interlocked.Exchange(ref _reportedPersistenceFailure, 0);
                if (Interlocked.Exchange(ref _firstPersisted, 1) == 0)
                    Console.WriteLine($"[CassandraLogWriter__Persist] First Cassandra record accepted; Application={_application}; Environment={_environment}; RetentionHours=48");
            }
            catch (Exception ex)
            {
                // Log one failure per outage; the sink must never trigger recursive logging.
                if (Interlocked.Exchange(ref _reportedPersistenceFailure, 1) == 0)
                    Console.Error.WriteLine($"[CassandraLogWriter__Persist] {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static bool IsVerbose(string level)
        {
            return String.Equals(level, "Trace", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(level, "Debug", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(level, "Verbose", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(level, "JSON", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(level, "JSONS", StringComparison.OrdinalIgnoreCase);
        }
    }
}
