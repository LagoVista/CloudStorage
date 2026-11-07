using LagoVista.IoT.Logging.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>
    /// Attaches Cassandra diagnostics after the host DI container is available.
    /// Bootstrap Logz.io remains primary. A dedicated DI scope keeps the activity
    /// store alive until shutdown and is disposed only after detaching the tap.
    /// </summary>
    public sealed class CassandraDiagnosticsBridgeHostedService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAdminLogger _adminLogger;
        private IServiceScope _scope;
        private LoggerBase _activeLogger;

        public CassandraDiagnosticsBridgeHostedService(IServiceScopeFactory scopeFactory, IAdminLogger adminLogger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _adminLogger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var logger = _adminLogger as LoggerBase ??
                throw new InvalidOperationException("Cassandra diagnostic mirror requires a LoggerBase host logger.");

            var scope = _scopeFactory.CreateScope();
            try
            {
                var writer = scope.ServiceProvider.GetRequiredService<CassandraLogWriter>();
                logger.ConfigureSecondaryLogWriter(writer);
                _activeLogger = logger;
                _scope = scope;
                Console.WriteLine("[CassandraDiagnostics__Bridge] Secondary writer attached; Logz.io remains primary.");
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Detach();
            return Task.CompletedTask;
        }

        private void Detach()
        {
            _activeLogger?.ConfigureSecondaryLogWriter(null);
            _activeLogger = null;
            _scope?.Dispose();
            _scope = null;
        }

        public void Dispose() => Detach();
    }
}
