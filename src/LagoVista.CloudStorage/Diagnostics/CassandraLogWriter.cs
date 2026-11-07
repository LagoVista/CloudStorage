using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Logging.Utils;
using System;
using System.Threading.Tasks;

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
        private readonly string _application;
        private readonly string _environment;
        private readonly string _version;

        public CassandraLogWriter(
            IActivityRecordStore<DiagnosticLogActivityRecord> store,
            IRuntimeControlService controls,
            string application,
            string environment,
            string version = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _controls = controls;
            _application = !String.IsNullOrWhiteSpace(application) ? application : throw new ArgumentNullException(nameof(application));
            _environment = !String.IsNullOrWhiteSpace(environment) ? environment : throw new ArgumentNullException(nameof(environment));
            _version = version;
        }

        public Task WriteError(LogRecord record) => WriteAsync(record, isError: true);
        public Task WriteEvent(LogRecord record) => WriteAsync(record, isError: false);

        private async Task WriteAsync(LogRecord record, bool isError)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            try
            {
                if (isError) await _console.WriteError(record).ConfigureAwait(false);
                else await _console.WriteEvent(record).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CassandraLogWriter__Console] {ex.GetType().Name}: {ex.Message}");
            }

            // Routine verbose diagnostics are opt-in; never gate errors or ordinary events.
            if (!isError && IsVerbose(record.LogLevel) &&
                _controls?.IsEnabled(RuntimeControlCatalog.LoggingVerbose) != true)
                return;

            try
            {
                var activity = DiagnosticLogActivityRecord.FromLogRecord(record, _application, _environment, _version);
                await _store.InsertAsync(activity).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CassandraLogWriter__Persist] Id={record.Id}; {ex.GetType().Name}: {ex.Message}");
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
